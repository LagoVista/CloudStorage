using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace LagoVista.CloudStorage.Storage.Migration
{
    public static class DocumentMigrationTransformer
    {
        private static readonly string[] _cosmosSystemFields = { "_rid", "_self", "_etag", "_attachments", "_ts" };

        public static bool TryTransform(JObject source, out BsonDocument target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            target = null;
            var id = GetString(source, "id");
            if (String.IsNullOrWhiteSpace(id)) return false;

            var copy = (JObject)source.DeepClone();
            RemoveProperty(copy, "id");
            foreach (var field in _cosmosSystemFields) RemoveProperty(copy, field);

            target = BsonDocument.Parse(copy.ToString(Formatting.None));
            target.InsertAt(0, new BsonElement("_id", id));
            return true;
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
