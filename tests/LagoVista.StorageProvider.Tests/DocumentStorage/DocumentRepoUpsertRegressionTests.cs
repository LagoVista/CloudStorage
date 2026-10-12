using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Exceptions;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    [TestClass]
    public class DocumentRepoUpsertRegressionTests
    {
        private sealed class TestEntity : EntityBase { }

        private sealed class TestRepo : DocumentDBRepoBase<TestEntity>
        {
            public TestRepo(IDocumentCloudServices services) : base(services) { }
            public Task<OperationResponse<TestEntity>> SaveAsync(TestEntity item) => UpsertDocumentAsync(item);
        }

        private static (TestRepo repo, Mock<IDocumentStorageClient> client) CreateRepo()
        {
            var client = new Mock<IDocumentStorageClient>();
            client.SetupGet(c => c.DatabaseName).Returns("UnitTest");
            client.Setup(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()))
                .ReturnsAsync((TestEntity entity, string tag) => new OperationResponse<TestEntity>(entity));
            var provider = new Mock<IDocumentStorageClientProvider>();
            provider.Setup(p => p.GetClient()).Returns(client.Object);
            var services = new Mock<IDocumentCloudServices>();
            services.SetupGet(s => s.AdminLogger).Returns(Mock.Of<IAdminLogger>());
            services.SetupGet(s => s.DependencyManager).Returns(Mock.Of<IDependencyManager>());
            services.SetupGet(s => s.DocumentStorageClientProvider).Returns(provider.Object);
            return (new TestRepo(services.Object), client);
        }

        private static TestEntity CreateEntity(string name) => new TestEntity
        {
            Id = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            Name = name,
            Key = "upsert-regression",
            OwnerOrganization = EntityHeader.Create("org", "Org")
        };

        [TestMethod]
        public async Task NewDocument_WithDependencyManager_UsesOptionalLookupBeforeUpsert()
        {
            var (repo, client) = CreateRepo();
            client.Setup(c => c.GetDocumentAsync<TestEntity>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<string, string, bool>((id, partition, throwOnNotFound) =>
                    throwOnNotFound
                        ? Task.FromException<TestEntity>(new RecordNotFoundException(nameof(TestEntity), id))
                        : Task.FromResult<TestEntity>(null));

            var entity = CreateEntity("New");
            await repo.SaveAsync(entity);

            client.Verify(c => c.GetDocumentAsync<TestEntity>(entity.Id, It.IsAny<string>(), false), Times.Once);
            client.Verify(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()), Times.Once);
            Assert.IsTrue(entity.AuditHistory == null || entity.AuditHistory.Count == 0);
        }

        [TestMethod]
        public async Task ExistingDocument_UnchangedName_DoesNotAppendRenameAudit()
        {
            var (repo, client) = CreateRepo();
            var entity = CreateEntity("Same");
            var existing = new TestEntity { Id = entity.Id, Name = "Same" };
            client.Setup(c => c.GetDocumentAsync<TestEntity>(entity.Id, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(existing);

            await repo.SaveAsync(entity);

            Assert.IsTrue(entity.AuditHistory == null || entity.AuditHistory.Count == 0);
            client.Verify(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ExistingDocument_Renamed_AddsRenameAudit()
        {
            var (repo, client) = CreateRepo();
            var entity = CreateEntity("New Name");
            var existing = new TestEntity { Id = entity.Id, Name = "Old Name" };
            client.Setup(c => c.GetDocumentAsync<TestEntity>(entity.Id, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(existing);

            await repo.SaveAsync(entity);

            Assert.IsNotNull(entity.AuditHistory);
            Assert.AreEqual(1, entity.AuditHistory.Count);
            Assert.AreEqual("Old Name", entity.AuditHistory[0].Changes[0].OldValue);
            Assert.AreEqual("New Name", entity.AuditHistory[0].Changes[0].NewValue);
        }

        [TestMethod]
        public async Task StorageFailure_DuringOptionalLookup_IsNotSwallowed()
        {
            var (repo, client) = CreateRepo();
            client.Setup(c => c.GetDocumentAsync<TestEntity>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repo.SaveAsync(CreateEntity("New")));
            client.Verify(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()), Times.Never);
        }
    }
}
