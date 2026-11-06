using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    [TestClass]
    public class ProducedArtifactGenerationOptOutTests
    {
        private sealed class StandardEntity : EntityBase { }

        [SkipProducedArtifactGeneration]
        private class ExcludedEntity : EntityBase { }

        private sealed class DerivedExcludedEntity : ExcludedEntity { }

        private sealed class TestRepo<TEntity> : DocumentDBRepoBase<TEntity>
            where TEntity : class, IEntityBase
        {
            public TestRepo(IDocumentCloudServices services) : base(services) { }

            public Task<OperationResponse<TEntity>> InsertAsync(TEntity entity) => CreateDocumentAsync(entity);

            public Task<OperationResponse<TEntity>> SaveAsync(TEntity entity) => UpsertDocumentAsync(entity);
        }

        private static (TestRepo<TEntity> repo, Mock<IDocumentStorageClient> storage, Mock<IProducedArtifactService> artifacts) CreateRepo<TEntity>()
            where TEntity : EntityBase
        {
            var storage = new Mock<IDocumentStorageClient>();
            storage.SetupGet(client => client.DatabaseName).Returns("UnitTest");
            storage.Setup(client => client.CreateDocumentAsync(It.IsAny<TEntity>()))
                .ReturnsAsync((TEntity entity) => new OperationResponse<TEntity>(entity));
            storage.Setup(client => client.UpsertDocumentAsync(It.IsAny<TEntity>(), It.IsAny<string>()))
                .ReturnsAsync((TEntity entity, string etag) => new OperationResponse<TEntity>(entity));

            var provider = new Mock<IDocumentStorageClientProvider>();
            provider.Setup(p => p.GetClient()).Returns(storage.Object);

            var artifacts = new Mock<IProducedArtifactService>();
            artifacts.Setup(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()))
                .ReturnsAsync(InvokeResult.Success);

            var services = new Mock<IDocumentCloudServices>();
            services.SetupGet(s => s.AdminLogger).Returns(Mock.Of<IAdminLogger>());
            services.SetupGet(s => s.DocumentStorageClientProvider).Returns(provider.Object);
            services.SetupGet(s => s.ProducedArtifactService).Returns(artifacts.Object);

            return (new TestRepo<TEntity>(services.Object), storage, artifacts);
        }

        private static TEntity NewEntity<TEntity>() where TEntity : EntityBase, new()
        {
            return new TEntity
            {
                Id = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                Name = "Test Entity",
                Key = "test-entity",
                OwnerOrganization = EntityHeader.Create("org", "Org")
            };
        }

        [TestMethod]
        public async Task MarkedEntity_Insert_StoresWithoutProducingArtifact()
        {
            var (repo, storage, artifacts) = CreateRepo<ExcludedEntity>();
            await repo.InsertAsync(NewEntity<ExcludedEntity>());

            storage.Verify(client => client.CreateDocumentAsync(It.IsAny<ExcludedEntity>()), Times.Once);
            artifacts.Verify(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()), Times.Never);
        }

        [TestMethod]
        public async Task MarkedEntity_Upsert_StoresWithoutProducingArtifact()
        {
            var (repo, storage, artifacts) = CreateRepo<ExcludedEntity>();
            await repo.SaveAsync(NewEntity<ExcludedEntity>());

            storage.Verify(client => client.UpsertDocumentAsync(It.IsAny<ExcludedEntity>(), It.IsAny<string>()), Times.Once);
            artifacts.Verify(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()), Times.Never);
        }

        [TestMethod]
        public async Task InheritedAttribute_Upsert_DoesNotProduceArtifact()
        {
            var (repo, storage, artifacts) = CreateRepo<DerivedExcludedEntity>();
            await repo.SaveAsync(NewEntity<DerivedExcludedEntity>());

            storage.Verify(client => client.UpsertDocumentAsync(It.IsAny<DerivedExcludedEntity>(), It.IsAny<string>()), Times.Once);
            artifacts.Verify(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()), Times.Never);
        }

        [TestMethod]
        public async Task UnmarkedEntity_Insert_ProducesArtifact()
        {
            var (repo, storage, artifacts) = CreateRepo<StandardEntity>();
            await repo.InsertAsync(NewEntity<StandardEntity>());

            storage.Verify(client => client.CreateDocumentAsync(It.IsAny<StandardEntity>()), Times.Once);
            artifacts.Verify(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()), Times.Once);
        }

        [TestMethod]
        public async Task UnmarkedEntity_Upsert_ProducesArtifact()
        {
            var (repo, storage, artifacts) = CreateRepo<StandardEntity>();
            await repo.SaveAsync(NewEntity<StandardEntity>());

            storage.Verify(client => client.UpsertDocumentAsync(It.IsAny<StandardEntity>(), It.IsAny<string>()), Times.Once);
            artifacts.Verify(service => service.CreateProducedArtifactsAsync(It.IsAny<IEntityBase>()), Times.Once);
        }
    }
}
