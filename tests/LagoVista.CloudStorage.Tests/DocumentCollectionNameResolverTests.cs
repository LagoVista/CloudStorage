using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using NUnit.Framework;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentCollectionNameResolverTests
    {
        [Test]
        public void Resolve_WithTwoTypesInSameDomain_ReturnsSameCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var first = resolver.Resolve("TestDb", typeof(FirstDomainEntity));
            var second = resolver.Resolve("TestDb", typeof(SecondDomainEntity));

            Assert.That(first, Is.EqualTo("SharedDomain"));
            Assert.That(second, Is.EqualTo("SharedDomain"));
        }

        [Test]
        public void Resolve_WithExplicitCollection_UsesExplicitCollection()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(FirstDomainEntity), "SpecialCollection");
            Assert.That(collection, Is.EqualTo("SpecialCollection"));
        }

        [Test]
        public void Resolve_WithMissingEntityDescription_UsesFallback()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(UnattributedEntity));
            Assert.That(collection, Is.EqualTo("TestDb_Collections"));
        }

        [Test]
        public void TryResolve_WithLoadedEntityType_UsesDomain()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", nameof(MigrationLookupEntity), out var collection);

            Assert.That(resolved, Is.True);
            Assert.That(collection, Is.EqualTo("MigrationDomain"));
        }

        [Test]
        public void TryResolve_WithUnknownEntityType_ReturnsVisibleFallback()
        {
            var resolver = new DocumentCollectionNameResolver();
            var resolved = resolver.TryResolve("TestDb", "DefinitelyNotARealEntityType", out var collection);

            Assert.That(resolved, Is.False);
            Assert.That(collection, Is.EqualTo("TestDb_Collections"));
        }

        [Test]
        public void Resolve_WithMongoReservedCharacters_NormalizesCollectionName()
        {
            var resolver = new DocumentCollectionNameResolver();
            var collection = resolver.Resolve("TestDb", typeof(ReservedCharacterDomainEntity));
            Assert.That(collection, Is.EqualTo("Domain_Name"));
        }

        [EntityDescription("SharedDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(DocumentCollectionNameResolverTests))]
        private sealed class FirstDomainEntity { }

        [EntityDescription("SharedDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(DocumentCollectionNameResolverTests))]
        private sealed class SecondDomainEntity { }

        [EntityDescription("MigrationDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(DocumentCollectionNameResolverTests))]
        private sealed class MigrationLookupEntity { }

        [EntityDescription("Domain$Name", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(DocumentCollectionNameResolverTests))]
        private sealed class ReservedCharacterDomainEntity { }

        private sealed class UnattributedEntity { }
    }
}
