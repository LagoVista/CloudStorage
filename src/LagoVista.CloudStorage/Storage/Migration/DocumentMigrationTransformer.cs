using LagoVista.Core.Models.UIMetaData;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace LagoVista.CloudStorage.Storage.Migration
{
    public sealed class DocumentMigrationTransformer
    {
        private static readonly string[] _cosmosSystemFields = { "_rid", "_self", "_etag", "_attachments", "_ts" };
        private readonly IEntityTypeResolver _entityTypeResolver;

        public DocumentMigrationTransformer(IEntityTypeResolver entityTypeResolver)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            MongoBsonSerialization.Configure();
        }

        public bool TryTransform(JObject source, out BsonDocument target)
        {
            return TryTransform(source, out target, out _);
        }

        public bool TryTransform(JObject source, out BsonDocument target, out string error)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            target = null;
            error = null;

            var id = GetString(source, "id");
            if (String.IsNullOrWhiteSpace(id))
            {
                error = "Document does not contain a non-empty id.";
                return false;
            }

            var entityType = GetString(source, "EntityType");
            if (String.IsNullOrWhiteSpace(entityType))
            {
                error = $"Document '{id}' does not contain a non-empty EntityType.";
                return false;
            }

            if (!_entityTypeResolver.TryGetEntityType(entityType, out var modelType) || modelType == null)
            {
                error = $"Could not resolve EntityType '{entityType}' for document '{id}'.";
                return false;
            }

            try
            {
                // Cosmos documents were produced through the Newtonsoft contract. Normalize the
                // source through that CLR contract first so computed/read-only JSON properties
                // are not blindly copied into Mongo.
                var copy = (JObject)source.DeepClone();
                foreach (var field in _cosmosSystemFields) RemoveProperty(copy, field);

                var model = Newtonsoft.Json.JsonConvert.DeserializeObject(copy.ToString(Formatting.None), modelType);
                if (model == null)
                {
                    error = $"Newtonsoft deserialization returned null for EntityType '{entityType}', document '{id}'.";
                    return false;
                }

                // Prepare the full CLR graph before Mongo resolves/freeze class maps so
                // shadowed CLR members and JSON payload members use the runtime compatibility serializers.
                MongoBsonSerialization.ConfigureForType(modelType);

                // Serialize through the exact Mongo serializer contract used at runtime.
                target = model.ToBsonDocument(modelType);

                if (!target.TryGetValue("_id", out var bsonId) ||
                    !String.Equals(bsonId.ToString(), id, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Mongo serialization changed or omitted id for EntityType '{entityType}', document '{id}'.";
                    target = null;
                    return false;
                }

                if (!target.TryGetValue("EntityType", out var bsonEntityType) ||
                    !String.Equals(bsonEntityType.AsString, entityType, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Mongo serialization changed or omitted EntityType '{entityType}' for document '{id}'.";
                    target = null;
                    return false;
                }

                // Prove the generated BSON can be materialized by the runtime Mongo serializer
                // before allowing it to be written.
                var serializer = BsonSerializer.LookupSerializer(modelType);
                using (var reader = new MongoDB.Bson.IO.BsonDocumentReader(target))
                {
                    var context = BsonDeserializationContext.CreateRoot(reader);
                    var args = new BsonDeserializationArgs { NominalType = modelType };
                    var roundTripped = serializer.Deserialize(context, args);
                    if (roundTripped == null)
                    {
                        error = $"Mongo round-trip deserialization returned null for EntityType '{entityType}', document '{id}'.";
                        target = null;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to transform EntityType '{entityType}', document '{id}': {ex.Message}";
                target = null;
                return false;
            }
        }

        private static string GetString(JObject document, string propertyName)
        {
            var property = document.Properties().FirstOrDefault(item => String.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return property?.Value?.Type == JTokenType.Null ? null : property?.Value?.ToString();
        }

        private static void RemoveProperty(JObject document, string propertyName)
        {
            var property = document.Properties().FirstOrDefault(item => String.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            property?.Remove();
        }
    }
}
