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

        [TestMethod]
        public async Task NewDocument_WithDependencyManager_UsesOptionalLookupBeforeUpsert()
        {
            var client = new Mock<IDocumentStorageClient>();
            client.SetupGet(c => c.DatabaseName).Returns("UnitTest");
            client.Setup(c => c.GetDocumentAsync<TestEntity>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<string, string, bool>((id, partition, throwOnNotFound) =>
                    throwOnNotFound
                        ? Task.FromException<TestEntity>(new RecordNotFoundException(nameof(TestEntity), id))
                        : Task.FromResult<TestEntity>(null));
            client.Setup(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()))
                .ReturnsAsync((TestEntity entity, string tag) => new OperationResponse<TestEntity>(entity));

            var provider = new Mock<IDocumentStorageClientProvider>();
            provider.Setup(p => p.GetClient()).Returns(client.Object);
            var services = new Mock<IDocumentCloudServices>();
            services.SetupGet(s => s.AdminLogger).Returns(Mock.Of<IAdminLogger>());
            services.SetupGet(s => s.DependencyManager).Returns(Mock.Of<IDependencyManager>());
            services.SetupGet(s => s.DocumentStorageClientProvider).Returns(provider.Object);

            var entity = new TestEntity
            {
                Id = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                Name = "New",
                Key = "new",
                OwnerOrganization = EntityHeader.Create("org", "Org")
            };
            await new TestRepo(services.Object).SaveAsync(entity);

            client.Verify(c => c.GetDocumentAsync<TestEntity>(entity.Id, It.IsAny<string>(), false), Times.Once);
            client.Verify(c => c.UpsertDocumentAsync(It.IsAny<TestEntity>(), It.IsAny<string>()), Times.Once);
        }
    }
}
