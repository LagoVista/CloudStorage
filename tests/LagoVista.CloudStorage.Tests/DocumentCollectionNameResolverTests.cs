using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentCollectionNameResolverTests
    {
        [Test]
        public void Resolve_WithShareableStorage_ReturnsEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(ShareableEntity));

            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.EntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithNoStorageAttribute_ReturnsEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(OrganizationEntity));

            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.EntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithDedicatedStorageCollection_ReturnsEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(DedicatedEntity));

            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.EntitiesCollectionName));
        }

        [Test]
        public void Resolve_WithExplicitCollection_UsesExplicitCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(OrganizationEntity), "SpecialCollection");

            Assert.That(collection, Is.EqualTo("SpecialCollection"));
        }

        [Test]
        public void TryResolve_WithLoadedEntity_UsesEntitiesCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", nameof(ShareableMigrationEntity), out var collection);

            Assert.That(resolved, Is.True);
            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.EntitiesCollectionName));
        }

        [Test]
        public void TryResolve_WithUnknownEntityType_ReturnsEntitiesFallback()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", "DefinitelyNotARealEntityType", out var collection);

            Assert.That(resolved, Is.False);
            Assert.That(collection, Is.EqualTo(DocumentCollectionNameResolver.EntitiesCollectionName));
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
        private sealed class ShareableMigrationEntity { }
    }
}
