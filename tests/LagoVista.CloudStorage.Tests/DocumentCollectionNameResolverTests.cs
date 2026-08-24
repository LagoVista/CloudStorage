using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentCollectionNameResolverTests
    {
        [Test]
        public void Resolve_WithShareableStorage_ReturnsSharedEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(ShareableEntity));

            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.SharedEntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithNoStorageAttribute_ReturnsOrganizationEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(OrganizationEntity));

            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.OrganizationEntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithDedicatedStorageCollection_ReturnsEntityTypeName()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(DedicatedEntity));

            Assert.That(collection, Is.EqualTo(nameof(DedicatedEntity)));
        }

        [Test]
        public void Resolve_WithExplicitCollection_UsesExplicitCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(OrganizationEntity), "SpecialCollection");

            Assert.That(collection, Is.EqualTo("SpecialCollection"));
        }

        [Test]
        public void Resolve_WithConflictingStorageAttributes_Throws()
        {
            var resolver = new DocumentCollectionNameResolver();

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("TestDb", typeof(InvalidStorageEntity)));
            Assert.That(exception.Message, Does.Contain(nameof(InvalidStorageEntity)));
        }

        [Test]
        public void TryResolve_WithLoadedShareableEntity_UsesSharedEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", nameof(ShareableMigrationEntity), out var collection);

            Assert.That(resolved, Is.True);
            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.SharedEntitiesCollectionName));
        }

        [Test]
        public void TryResolve_WithLoadedOrganizationEntity_UsesOrganizationEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", nameof(OrganizationMigrationEntity), out var collection);

            Assert.That(resolved, Is.True);
            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.OrganizationEntitiesCollectionName));
        }

        [Test]
        public void TryResolve_WithLoadedDedicatedEntity_UsesEntityTypeName()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", nameof(DedicatedMigrationEntity), out var collection);

            Assert.That(resolved, Is.True);
            Assert.That(collection, Is.EqualTo(nameof(DedicatedMigrationEntity)));
        }

        [Test]
        public void TryResolve_WithUnknownEntityType_ReturnsOrganizationFallback()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", "DefinitelyNotARealEntityType", out var collection);

            Assert.That(resolved, Is.False);
            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.OrganizationEntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithExplicitMongoReservedCharacters_NormalizesCollectionName()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(OrganizationEntity), "Domain$Name");

            Assert.That(collection, Is.EqualTo("Domain_Name"));
        }

        [ShareableStorage]
        private sealed class ShareableEntity { }

        private sealed class OrganizationEntity { }

        [DedicatedStorageCollection]
        private sealed class DedicatedEntity { }

        [ShareableStorage]
        [DedicatedStorageCollection]
        private sealed class InvalidStorageEntity { }

        [ShareableStorage]
        private sealed class ShareableMigrationEntity { }

        private sealed class OrganizationMigrationEntity { }

        [DedicatedStorageCollection]
        private sealed class DedicatedMigrationEntity { }
    }
}
