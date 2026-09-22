using LagoVista.CloudStorage.Storage.StorageProviders;
using LagoVista.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    public class DocumentCollectionNameResolverTests
    {
        [CollectionName("MediaResources")]
        private sealed class RoutedEntity
        {
        }

        private sealed class DefaultEntity
        {
        }

        [TestMethod]
        public void Resolve_UsesCollectionNameAttribute()
        {
            var resolver = new DocumentCollectionNameResolver();

            var collectionName = resolver.Resolve("dev", typeof(RoutedEntity));

            Assert.AreEqual("MediaResources", collectionName);
        }

        [TestMethod]
        public void Resolve_UsesEntitiesFallbackWithoutCollectionNameAttribute()
        {
            var resolver = new DocumentCollectionNameResolver();

            var collectionName = resolver.Resolve("dev", typeof(DefaultEntity));

            Assert.AreEqual(DocumentCollectionNameResolver.EntitiesCollectionName, collectionName);
        }

        [TestMethod]
        public void TryResolve_UsesCollectionNameAttributeForLoadedEntityType()
        {
            var resolver = new DocumentCollectionNameResolver();

            var resolved = resolver.TryResolve("dev", nameof(RoutedEntity), out var collectionName);

            Assert.IsTrue(resolved);
            Assert.AreEqual("MediaResources", collectionName);
        }
    }
}
