using LagoVista;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Linq.Expressions;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public sealed class MongoGuidString36FilterTranslationTests
    {
        private sealed class ProductQueryDocument
        {
            public EntityHeader ProductCategory { get; set; }
        }

        [TestMethod]
        public void ProductCategoryIdComparedToGuidString36_RendersStringFieldFilter()
        {
            MongoBsonSerialization.Configure();
            var categoryId = GuidString36.Factory();
            var query = BuildLegacyProductCategoryComparison(categoryId, ExpressionType.Equal, reverse: false);

            var normalized = MongoDocumentStorageClient.NormalizeGuidString36Comparisons(query);
            Assert.IsFalse(normalized.ToString().Contains("Convert("));

            var bson = Builders<ProductQueryDocument>.Filter.Where(normalized).Render(
                new RenderArgs<ProductQueryDocument>(
                    BsonSerializer.LookupSerializer<ProductQueryDocument>(),
                    BsonSerializer.SerializerRegistry)).ToJson();

            StringAssert.Contains(bson, "ProductCategory.Id");
            StringAssert.Contains(bson, categoryId.Value);
        }

        [TestMethod]
        public void ReverseNotEqualComparison_RendersWithoutGuidConversion()
        {
            MongoBsonSerialization.Configure();
            var categoryId = GuidString36.Factory();
            var query = BuildLegacyProductCategoryComparison(categoryId, ExpressionType.NotEqual, reverse: true);

            var normalized = MongoDocumentStorageClient.NormalizeGuidString36Comparisons(query);
            Assert.IsFalse(normalized.ToString().Contains("Convert("));

            var bson = Builders<ProductQueryDocument>.Filter.Where(normalized).Render(
                new RenderArgs<ProductQueryDocument>(
                    BsonSerializer.LookupSerializer<ProductQueryDocument>(),
                    BsonSerializer.SerializerRegistry)).ToJson();

            StringAssert.Contains(bson, "ProductCategory.Id");
            StringAssert.Contains(bson, categoryId.Value);
        }

        [TestMethod]
        public void OrdinaryStringComparison_RemainsUnchanged()
        {
            Expression<Func<ProductQueryDocument, bool>> query =
                product => product.ProductCategory.Id == "CAT-A";

            var normalized = MongoDocumentStorageClient.NormalizeGuidString36Comparisons(query);
            Assert.AreSame(query, normalized);
        }

        private static Expression<Func<ProductQueryDocument, bool>> BuildLegacyProductCategoryComparison(
            GuidString36 categoryId, ExpressionType comparison, bool reverse)
        {
            var product = Expression.Parameter(typeof(ProductQueryDocument), "prd");
            var category = Expression.Property(product, nameof(ProductQueryDocument.ProductCategory));
            var idField = Expression.Property(category, nameof(EntityHeader.Id));
            var convertedField = Expression.Convert(idField, typeof(GuidString36));
            var capturedGuid = Expression.Constant(categoryId, typeof(GuidString36));
            var predicate = reverse
                ? Expression.MakeBinary(comparison, capturedGuid, convertedField)
                : Expression.MakeBinary(comparison, convertedField, capturedGuid);

            return Expression.Lambda<Func<ProductQueryDocument, bool>>(predicate, product);
        }
    }
}
