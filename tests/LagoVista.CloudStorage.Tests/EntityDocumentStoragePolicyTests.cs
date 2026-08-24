using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class EntityDocumentStoragePolicyTests
    {
        [Test]
        public void ValidateForWrite_ShareableEntity_AllowsPublic()
        {
            var entity = CreateEntity<ShareableEntity>();
            entity.IsPublic = true;

            Assert.DoesNotThrow(() => EntityDocumentStoragePolicy.ValidateForWrite(entity));
        }

        [Test]
        public void ValidateForWrite_DefaultEntity_RejectsPublic()
        {
            var entity = CreateEntity<OrganizationEntity>();
            entity.IsPublic = true;

            Assert.Throws<InvalidOperationException>(() => EntityDocumentStoragePolicy.ValidateForWrite(entity));
        }

        [Test]
        public void ValidateForWrite_DedicatedEntity_RejectsPublic()
        {
            var entity = CreateEntity<DedicatedEntity>();
            entity.IsPublic = true;

            Assert.Throws<InvalidOperationException>(() => EntityDocumentStoragePolicy.ValidateForWrite(entity));
        }

        [Test]
        public void ValidateForWrite_RequiresOwnerOrganization()
        {
            var entity = new OrganizationEntity();

            Assert.Throws<InvalidOperationException>(() => EntityDocumentStoragePolicy.ValidateForWrite(entity));
        }

        [Test]
        public void CosmosPartitionKeyPath_IsOwnerOrganizationId()
        {
            Assert.That(EntityDocumentStoragePolicy.CosmosPartitionKeyPath, Is.EqualTo("/OwnerOrganization/Id"));
        }

        private static TEntity CreateEntity<TEntity>() where TEntity : EntityBase, new()
        {
            return new TEntity
            {
                Id = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                Name = typeof(TEntity).Name,
                OwnerOrganization = EntityHeader.Create("ORG1", "Organization One")
            };
        }

        [ShareableStorage]
        private sealed class ShareableEntity : EntityBase { }

        private sealed class OrganizationEntity : EntityBase { }

        [DedicatedStorageCollection]
        private sealed class DedicatedEntity : EntityBase { }
    }
}
