using LagoVista;
using LagoVista.Core.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Linq;

namespace LagoVista.CloudStorage.Storage.Migration
{
    public sealed class DocumentMigrationTransformer
    {
        private static readonly string[] _cosmosSystemFields = { "_rid", "_self", "_etag", "_attachments", "_ts" };
        private static readonly string[] _legacyUtcFormats =
        {
            "M/d/yyyy H:mm:ss",
            "M/d/yyyy H:mm:ss.FFFFFFF",
            "M/d/yyyy h:mm:ss tt",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
        };
        private static readonly DefaultContractResolver _contractResolver = new DefaultContractResolver();
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

                // Historical Cosmos documents can contain UTC values emitted by older serializers
                // as invariant date/time strings without the required trailing Z. Normalize only
                // JSON values whose current CLR contract is UtcTimestamp so unrelated strings are
                // never rewritten by migration.
                NormalizeLegacyUtcTimestamps(copy, modelType);
                NormalizeEmbeddedStateSetKeys(copy, modelType);

                var model = Newtonsoft.Json.JsonConvert.DeserializeObject(copy.ToString(Formatting.None), modelType);
                if (model == null)
                {
                    error = $"Newtonsoft deserialization returned null for EntityType '{entityType}', document '{id}'.";
                    return false;
                }

                // Cosmos' document id is authoritative. Some legacy records also contain a
                // stale StoredId value (including GuidString36 values) that can overwrite the
                // JsonProperty("id") value during Newtonsoft materialization. Re-assert the
                // canonical document id before BSON serialization.
                var expectedSerializedId = id;
                if (model is IIDEntity idEntity)
                {
                    if (NormalizedId32.TryCreate(id, out var normalizedId))
                    {
                        idEntity.Id = normalizedId;
                    }
                    else if (AllowsLegacyGuidDocumentId(modelType) && GuidString36.IsStrictLowerD(id))
                    {
                        // ProductEntity is a deliberate legacy exception. Preserve the historical
                        // Cosmos document id byte-for-byte as Mongo _id. EntityBase.Id still exposes
                        // the canonical NormalizedId32 form to application code when the model is read.
                        expectedSerializedId = id;
                    }
                    else
                    {
                        error = $"Document id '{id}' for EntityType '{entityType}' is not a valid NormalizedId32.";
                        return false;
                    }
                }

                // Prepare the full CLR graph before Mongo resolves/freeze class maps so
                // shadowed CLR members and JSON payload members use the runtime compatibility serializers.
                MongoBsonSerialization.ConfigureForType(modelType);

                // Serialize through the exact Mongo serializer contract used at runtime.
                target = model.ToBsonDocument(modelType);

                if (!target.TryGetValue("_id", out var bsonId))
                {
                    error = $"Mongo serialization omitted _id for EntityType '{entityType}', document '{id}'.";
                    target = null;
                    return false;
                }

                var serializedId = bsonId.IsString ? bsonId.AsString : bsonId.ToString();
                if (!String.Equals(serializedId, expectedSerializedId, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Mongo serialization changed id for EntityType '{entityType}', document '{id}'. Expected _id '{expectedSerializedId}', serialized _id was '{serializedId}'.";
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

        private static void NormalizeLegacyUtcTimestamps(JToken token, Type declaredType)
        {
            if (token == null || declaredType == null) return;

            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (targetType == typeof(UtcTimestamp))
            {
                if (token is JValue value && value.Type == JTokenType.String)
                {
                    var text = value.Value?.ToString();
                    if (!String.IsNullOrWhiteSpace(text) && !text.EndsWith("Z", StringComparison.Ordinal))
                    {
                        if (DateTime.TryParseExact(
                            text,
                            _legacyUtcFormats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out var parsed))
                        {
                            value.Value = DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
                        }
                    }
                }

                return;
            }

            var contract = _contractResolver.ResolveContract(targetType);
            if (token is JObject document && contract is JsonObjectContract objectContract)
            {
                foreach (var property in objectContract.Properties)
                {
                    if (property.PropertyType == null) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    NormalizeLegacyUtcTimestamps(jsonProperty.Value, property.PropertyType);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                foreach (var item in array)
                    NormalizeLegacyUtcTimestamps(item, arrayContract.CollectionItemType);
            }
        }

        private static void NormalizeEmbeddedStateSetKeys(JToken token, Type declaredType)
        {
            if (token == null || declaredType == null) return;

            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (targetType.IsGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(LagoVista.Core.Models.EntityHeader<>) &&
                targetType.GetGenericArguments()[0].Name == "StateSet" &&
                token is JObject header)
            {
                var headerId = GetString(header, "Id");
                var valueProperty = header.Properties().FirstOrDefault(item =>
                    String.Equals(item.Name, "Value", StringComparison.OrdinalIgnoreCase));

                if (valueProperty?.Value is JObject value)
                {
                    var keyProperty = value.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, "Key", StringComparison.OrdinalIgnoreCase));

                    var missingKey = keyProperty == null ||
                        keyProperty.Value.Type == JTokenType.Null ||
                        String.IsNullOrWhiteSpace(keyProperty.Value.ToString());

                    if (missingKey)
                    {
                        string replacementKey = null;

                        // Prefer the EntityHeader id when older embedded snapshots retained it.
                        if (!String.IsNullOrWhiteSpace(headerId) && LagoVistaKey.TryCreate(headerId, out _))
                        {
                            replacementKey = headerId;
                        }
                        else
                        {
                            // Some very old Device snapshots have null header Id/Text and a populated
                            // StateSet.Value.id. Use a deterministic migration-only key derived from
                            // that persisted identity rather than dropping the embedded StateSet.
                            var valueId = GetString(value, "id");
                            if (NormalizedId32.TryCreate(valueId, out var normalizedValueId))
                                replacementKey = $"stateset-{normalizedValueId.Value.ToLowerInvariant()}";
                        }

                        if (!String.IsNullOrWhiteSpace(replacementKey))
                        {
                            if (keyProperty == null)
                                value.Add("Key", replacementKey);
                            else
                                keyProperty.Value = replacementKey;
                        }
                    }
                }
            }

            var contract = _contractResolver.ResolveContract(targetType);
            if (token is JObject document && contract is JsonObjectContract objectContract)
            {
                foreach (var property in objectContract.Properties)
                {
                    if (property.PropertyType == null) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    NormalizeEmbeddedStateSetKeys(jsonProperty.Value, property.PropertyType);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                foreach (var item in array)
                    NormalizeEmbeddedStateSetKeys(item, arrayContract.CollectionItemType);
            }
        }

        private static bool AllowsLegacyGuidDocumentId(Type modelType) =>
            Attribute.IsDefined(modelType, typeof(AllowLegacyGuidDocumentIdAttribute), inherit: true);

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
