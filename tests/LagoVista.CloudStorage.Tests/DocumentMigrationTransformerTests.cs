using LagoVista.CloudStorage.DocumentDB;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentMigrationTransformerTests
    {
        [Test]
        public void TryTransform_WithCosmosDocument_MovesIdAndStripsSystemFields()
        {
            var source = JObject.Parse(@"{
                'id':'ABC123',
                'EntityType':'WorkTask',
                'Name':'Test',
                '_rid':'rid-value',
                '_self':'self-value',
                '_etag':'etag-value',
                '_attachments':'attachments/',
                '_ts':123456,
                'Nested':{'Id':'NESTED-ID'}
            }");

            var transformed = DocumentMigrationTransformer.TryTransform(source, out var target);

            Assert.That(transformed, Is.True);
            Assert.That(target["_id"].AsString, Is.EqualTo("ABC123"));
            Assert.That(target.Contains("id"), Is.False);
            Assert.That(target.Contains("_rid"), Is.False);
            Assert.That(target.Contains("_self"), Is.False);
            Assert.That(target.Contains("_etag"), Is.False);
            Assert.That(target.Contains("_attachments"), Is.False);
            Assert.That(target.Contains("_ts"), Is.False);
            Assert.That(target["EntityType"].AsString, Is.EqualTo("WorkTask"));
            Assert.That(target["Name"].AsString, Is.EqualTo("Test"));
            Assert.That(target["Nested"].AsBsonDocument["Id"].AsString, Is.EqualTo("NESTED-ID"));
        }

        [Test]
        public void TryTransform_WithMixedCaseCosmosFields_StripsFieldsCaseInsensitively()
        {
            var source = JObject.Parse(@"{'ID':'ABC123','EntityType':'WorkTask','_ETAG':'etag-value','_TS':123456}");

            var transformed = DocumentMigrationTransformer.TryTransform(source, out var target);

            Assert.That(transformed, Is.True);
            Assert.That(target["_id"].AsString, Is.EqualTo("ABC123"));
            Assert.That(target.Contains("ID"), Is.False);
            Assert.That(target.Contains("_ETAG"), Is.False);
            Assert.That(target.Contains("_TS"), Is.False);
        }

        [Test]
        public void TryTransform_WithoutId_ReturnsFalse()
        {
            var source = JObject.Parse(@"{'EntityType':'WorkTask','Name':'Missing Id'}");

            var transformed = DocumentMigrationTransformer.TryTransform(source, out var target);

            Assert.That(transformed, Is.False);
            Assert.That(target, Is.Null);
        }

        [Test]
        public void TryTransform_DoesNotMutateSourceDocument()
        {
            var source = JObject.Parse(@"{'id':'ABC123','EntityType':'WorkTask','_etag':'etag-value'}");

            DocumentMigrationTransformer.TryTransform(source, out _);

            Assert.That(source.Value<string>("id"), Is.EqualTo("ABC123"));
            Assert.That(source.Value<string>("_etag"), Is.EqualTo("etag-value"));
        }
    }
}
