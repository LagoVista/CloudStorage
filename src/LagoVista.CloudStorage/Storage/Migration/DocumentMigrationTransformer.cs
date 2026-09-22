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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
                NormalizeLegacyOrgNamespaces(copy, modelType);
                NormalizeLegacyLagoVistaKeys(copy, modelType, id, "$");
                NormalizeMissingNestedNormalizedIds(copy, modelType, id, "$", true);
                NormalizeEmbeddedStateSetKeys(copy, modelType);

                var model = Newtonsoft.Json.JsonConvert.DeserializeObject(copy.ToString(Formatting.None), modelType);
                if (model == null)
                {
                    error = $"Newtonsoft deserialization returned null for EntityType '{entityType}', document '{id}'.";
                    return false;
                }

                NormalizeObjectPayloads(model, modelType, new HashSet<object>(ReferenceEqualityComparer.Instance));

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

        private static void NormalizeLegacyOrgNamespaces(JToken token, Type declaredType)
        {
            if (token == null || declaredType == null) return;

            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (targetType == typeof(OrgNamespace))
            {
                if (token is JValue value && value.Type == JTokenType.String)
                {
                    var text = value.Value?.ToString();
                    if (!String.IsNullOrWhiteSpace(text) && !OrgNamespace.IsValid(text))
                    {
                        var normalized = NormalizeLegacyOrgNamespaceValue(text);
                        if (OrgNamespace.IsValid(normalized))
                            value.Value = normalized;
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

                    NormalizeLegacyOrgNamespaces(jsonProperty.Value, property.PropertyType);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                foreach (var item in array)
                    NormalizeLegacyOrgNamespaces(item, arrayContract.CollectionItemType);
            }
        }

        private static string NormalizeLegacyOrgNamespaceValue(string value)
        {
            var trimmed = (value ?? String.Empty).Trim().ToLowerInvariant();
            var decomposed = trimmed.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                    builder.Append(ch);
            }

            var normalized = builder.ToString();
            if (String.IsNullOrWhiteSpace(normalized))
            {
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(trimmed));
                    normalized = "org" + BitConverter.ToString(hash, 0, 6).Replace("-", String.Empty).ToLowerInvariant();
                }
            }
            else if (normalized[0] < 'a' || normalized[0] > 'z')
            {
                normalized = "org" + normalized;
            }

            while (normalized.Length < 6)
                normalized += "nsp";

            if (normalized.Length > 64)
                normalized = normalized.Substring(0, 64);

            return normalized;
        }

        private static void NormalizeLegacyLagoVistaKeys(JToken token, Type declaredType, string documentId, string path)
        {
            if (token == null || declaredType == null) return;

            var nullableType = Nullable.GetUnderlyingType(declaredType);
            var targetType = nullableType ?? declaredType;

            if (targetType == typeof(LagoVistaKey))
            {
                if (nullableType != null && (token.Type == JTokenType.Null ||
                    (token.Type == JTokenType.String && String.IsNullOrWhiteSpace(token.ToString()))))
                    return;

                if (token is JValue value)
                {
                    var source = value.Type == JTokenType.Null ? null : value.Value?.ToString();
                    if (!TryParseLagoVistaKey(source, out _))
                    {
                        value.Value = CreateDeterministicLegacyKey(source, documentId, path);
                    }
                }

                return;
            }

            var contract = _contractResolver.ResolveContract(targetType);
            if (token is JObject document && contract is JsonObjectContract objectContract)
            {
                foreach (var property in objectContract.Properties)
                {
                    if (property.PropertyType == null || property.Ignored) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    NormalizeLegacyLagoVistaKeys(
                        jsonProperty.Value,
                        property.PropertyType,
                        documentId,
                        path + "." + (property.UnderlyingName ?? property.PropertyName));
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                for (var index = 0; index < array.Count; index++)
                    NormalizeLegacyLagoVistaKeys(array[index], arrayContract.CollectionItemType, documentId, path + "[" + index + "]");
            }
        }

        private static bool TryParseLagoVistaKey(string value, out LagoVistaKey key)
        {
            try
            {
                key = new LagoVistaKey(value);
                return true;
            }
            catch
            {
                key = default(LagoVistaKey);
                return false;
            }
        }

        private static string CreateDeterministicLegacyKey(string value, string documentId, string path)
        {
            var raw = (value ?? String.Empty).Trim().ToLowerInvariant();
            var builder = new StringBuilder();
            var lastWasDash = false;

            foreach (var ch in raw.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                    lastWasDash = false;
                }
                else if (!lastWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }

            var candidate = builder.ToString().Trim('-');
            if (String.IsNullOrWhiteSpace(candidate))
                candidate = "key";

            if (candidate[0] < 'a' || candidate[0] > 'z')
                candidate = "k-" + candidate;

            while (candidate.Length < 3)
                candidate += "k";

            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes((documentId ?? String.Empty) + "|" + (path ?? String.Empty) + "|" + raw));
                var suffix = BitConverter.ToString(hashBytes, 0, 4).Replace("-", String.Empty).ToLowerInvariant();

                // If the source was invalid, include a short deterministic suffix so normalization
                // cannot silently collapse distinct historical keys to the same value.
                var maxBaseLength = 128 - suffix.Length - 1;
                if (candidate.Length > maxBaseLength)
                    candidate = candidate.Substring(0, maxBaseLength).TrimEnd('-');

                candidate = candidate + "-" + suffix;
            }

            return candidate;
        }

        private static void NormalizeObjectPayloads(object instance, Type declaredType, HashSet<object> visited)
        {
            if (instance == null || declaredType == null) return;
            if (instance is string || declaredType.IsValueType) return;
            if (!visited.Add(instance)) return;

            if (instance is IDictionary<string, object> objectDictionary)
            {
                foreach (var key in objectDictionary.Keys.ToList())
                    objectDictionary[key] = ConvertJTokenPayload(objectDictionary[key]);

                return;
            }

            if (instance is IEnumerable enumerable && !(instance is string))
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                        NormalizeObjectPayloads(item, item.GetType(), visited);
                }

                return;
            }

            foreach (var property in declaredType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 || property.GetMethod == null) continue;

                var value = property.GetValue(instance);
                if (value == null) continue;

                if (value is IDictionary<string, object> dictionary)
                {
                    foreach (var key in dictionary.Keys.ToList())
                        dictionary[key] = ConvertJTokenPayload(dictionary[key]);
                }
                else
                {
                    NormalizeObjectPayloads(value, property.PropertyType, visited);
                }
            }
        }

        private static object ConvertJTokenPayload(object value)
        {
            if (!(value is JToken token)) return value;

            switch (token.Type)
            {
                case JTokenType.Object:
                    return ((JObject)token).Properties()
                        .ToDictionary(property => property.Name, property => ConvertJTokenPayload(property.Value), StringComparer.Ordinal);

                case JTokenType.Array:
                    return ((JArray)token).Select(item => ConvertJTokenPayload(item)).ToList();

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    return token is JValue scalar ? scalar.Value : token.ToString(Formatting.None);
            }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y) => Object.ReferenceEquals(x, y);

            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static void NormalizeLegacyLagoVistaKeys(JToken token, Type declaredType)
        {
            if (token == null || declaredType == null) return;

            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (targetType == typeof(LagoVistaKey))
            {
                if (token is JValue value && value.Type == JTokenType.String)
                {
                    var text = value.Value?.ToString();
                    if (!String.IsNullOrWhiteSpace(text))
                    {
                        try
                        {
                            _ = new LagoVistaKey(text);
                        }
                        catch (FormatException)
                        {
                            var normalized = NormalizeLegacyLagoVistaKeyValue(text);
                            _ = new LagoVistaKey(normalized);
                            value.Value = normalized;
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
                    if (property.PropertyType == null || property.Ignored) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    NormalizeLegacyLagoVistaKeys(jsonProperty.Value, property.PropertyType);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                foreach (var item in array)
                    NormalizeLegacyLagoVistaKeys(item, arrayContract.CollectionItemType);
            }
        }

        private static string NormalizeLegacyLagoVistaKeyValue(string value)
        {
            var decomposed = (value ?? String.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            var pendingDash = false;

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                var valid = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
                if (valid)
                {
                    if (pendingDash && builder.Length > 0 && builder[builder.Length - 1] != '-')
                        builder.Append('-');

                    builder.Append(ch);
                    pendingDash = false;
                }
                else
                {
                    pendingDash = true;
                }
            }

            var normalized = builder.ToString().Trim('-');
            if (String.IsNullOrWhiteSpace(normalized))
                normalized = "legacy-key";

            if (normalized[0] < 'a' || normalized[0] > 'z')
                normalized = "key-" + normalized;

            while (normalized.Length < 3)
                normalized += "key";

            if (normalized.Length > 128)
                normalized = normalized.Substring(0, 128).TrimEnd('-');

            return normalized;
        }

        private static void NormalizeLegacyEnumEntityHeaders(JToken token, Type declaredType)
        {
            if (token == null || declaredType == null) return;

            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (targetType.IsGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(LagoVista.Core.Models.EntityHeader<>) &&
                targetType.GetGenericArguments()[0].GetTypeInfo().IsEnum &&
                token is JObject header)
            {
                var enumType = targetType.GetGenericArguments()[0];
                var idProperty = header.Properties().FirstOrDefault(item => String.Equals(item.Name, "Id", StringComparison.OrdinalIgnoreCase));
                var id = idProperty?.Value?.Type == JTokenType.Null ? null : idProperty?.Value?.ToString();

                if (!String.IsNullOrWhiteSpace(id) && !IsValidEnumHeaderId(enumType, id))
                {
                    var key = GetString(header, "Key");
                    var text = GetString(header, "Text");

                    if (TryResolveEnumHeaderId(enumType, key, text, id, out var resolved))
                    {
                        idProperty.Value = resolved;
                        var keyProperty = header.Properties().FirstOrDefault(item => String.Equals(item.Name, "Key", StringComparison.OrdinalIgnoreCase));
                        if (keyProperty == null)
                            header.Add("Key", resolved);
                        else if (keyProperty.Value.Type == JTokenType.Null || String.IsNullOrWhiteSpace(keyProperty.Value.ToString()))
                            keyProperty.Value = resolved;
                    }
                }
            }

            var contract = _contractResolver.ResolveContract(targetType);
            if (token is JObject document && contract is JsonObjectContract objectContract)
            {
                foreach (var property in objectContract.Properties)
                {
                    if (property.PropertyType == null || property.Ignored) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    NormalizeLegacyEnumEntityHeaders(jsonProperty.Value, property.PropertyType);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                foreach (var item in array)
                    NormalizeLegacyEnumEntityHeaders(item, arrayContract.CollectionItemType);
            }
        }

        private static bool IsValidEnumHeaderId(Type enumType, string id)
        {
            foreach (var enumValue in Enum.GetValues(enumType))
            {
                var member = enumType.GetTypeInfo().DeclaredMembers.FirstOrDefault(item => item.Name == enumValue.ToString());
                var attr = member?.GetCustomAttribute<EnumLabelAttribute>();
                var expected = attr?.Key ?? enumValue.ToString();
                if (String.Equals(expected, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool TryResolveEnumHeaderId(Type enumType, string key, string text, string id, out string resolved)
        {
            foreach (var enumValue in Enum.GetValues(enumType))
            {
                var member = enumType.GetTypeInfo().DeclaredMembers.FirstOrDefault(item => item.Name == enumValue.ToString());
                var attr = member?.GetCustomAttribute<EnumLabelAttribute>();
                var expected = attr?.Key ?? enumValue.ToString();

                string label = null;
                if (attr?.ResourceType != null && !String.IsNullOrWhiteSpace(attr.LabelResource))
                {
                    var labelProperty = attr.ResourceType.GetTypeInfo().GetDeclaredProperty(attr.LabelResource);
                    label = labelProperty?.GetValue(labelProperty.DeclaringType, null) as string;
                }

                if (String.Equals(key, expected, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(key, enumValue.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(text, expected, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(text, enumValue.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    (!String.IsNullOrWhiteSpace(label) && String.Equals(text, label, StringComparison.OrdinalIgnoreCase)) ||
                    String.Equals(id, enumValue.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    resolved = expected;
                    return true;
                }
            }

            resolved = null;
            return false;
        }

        private static void NormalizeMissingNestedNormalizedIds(JToken token, Type declaredType, string documentId, string path, bool isRoot)
        {
            if (token == null || declaredType == null) return;

            var nullableType = Nullable.GetUnderlyingType(declaredType);
            var targetType = nullableType ?? declaredType;

            if (targetType == typeof(NormalizedId32))
            {
                // Nullable identifiers are allowed to remain null/empty. For non-nullable
                // nested identifiers, historical Cosmos documents occasionally persisted
                // explicit null/empty values even though current constructors always create
                // an id. Repair those deterministically from the document id + CLR path.
                if (nullableType == null && !isRoot && token is JValue value &&
                    (value.Type == JTokenType.Null ||
                     (value.Type == JTokenType.String && String.IsNullOrWhiteSpace(value.Value?.ToString()))))
                {
                    value.Value = CreateDeterministicNormalizedId(documentId, path);
                }

                return;
            }

            var contract = _contractResolver.ResolveContract(targetType);
            if (token is JObject document && contract is JsonObjectContract objectContract)
            {
                foreach (var property in objectContract.Properties)
                {
                    if (property.PropertyType == null || property.Ignored) continue;

                    var jsonProperty = document.Properties().FirstOrDefault(item =>
                        String.Equals(item.Name, property.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(item.Name, property.UnderlyingName, StringComparison.OrdinalIgnoreCase));
                    if (jsonProperty == null) continue;

                    var childPath = path + "." + (property.UnderlyingName ?? property.PropertyName);
                    NormalizeMissingNestedNormalizedIds(jsonProperty.Value, property.PropertyType, documentId, childPath, false);
                }

                return;
            }

            if (token is JArray array && contract is JsonArrayContract arrayContract && arrayContract.CollectionItemType != null)
            {
                for (var index = 0; index < array.Count; index++)
                    NormalizeMissingNestedNormalizedIds(array[index], arrayContract.CollectionItemType, documentId, path + "[" + index + "]", false);
            }
        }

        private static string CreateDeterministicNormalizedId(string documentId, string path)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((documentId ?? String.Empty) + "|" + (path ?? String.Empty)));
                var builder = new StringBuilder(32);
                for (var i = 0; i < 16; i++)
                    builder.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));

                return builder.ToString();
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
