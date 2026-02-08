using System;
using System.Linq;
using System.Text.RegularExpressions;
using LagoVista.Core.Models;
using Newtonsoft.Json.Linq;

namespace LagoVista.CloudStorage.Utils
{

    public static class EntityHeaderJsonPathExtractor
    {
        // Example: /Roles/@id=986B94D956AD415C84502B898A4CC904
        private static readonly Regex _expr =
            new Regex(@"^\s*/(?<node>[A-Za-z0-9_]+)/@id=(?<id>[A-Za-z0-9]+)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static EntityHeader TryExtract(string json, string expression)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            var root = JObject.Parse(json);
            return TryExtract(root, expression);
        }

        public static EntityHeader TryExtract(JObject root, string expression)
        {
            if (root == null || String.IsNullOrWhiteSpace(expression)) return null;

            var match = _expr.Match(expression);
            if (!match.Success) return null;

            var nodeName = match.Groups["node"].Value;
            var id = match.Groups["id"].Value;

            // Node must exist and be an array (e.g., Roles: [ { ... }, ... ])
            var nodeToken = GetPropertyCaseInsensitive(root, nodeName);
            if (!(nodeToken is JArray arr))
                return null;

            // Find entity where Id matches
            var entity = arr
                .OfType<JObject>()
                .FirstOrDefault(o =>
                    String.Equals(GetStringCaseInsensitive(o, "Id"), id, StringComparison.OrdinalIgnoreCase));

            if (entity == null) return null;

            var foundId = GetStringCaseInsensitive(entity, "Id");
            if (String.IsNullOrWhiteSpace(foundId)) return null;

            // Text = Name (per your requirement) with optional fallbacks to keep it resilient
            var text =
                GetStringCaseInsensitive(entity, "Name")
                ?? GetStringCaseInsensitive(entity, "Text")
                ?? GetStringCaseInsensitive(entity, "Key")
                ?? foundId;

            return EntityHeader.Create(foundId, text);
        }

        private static JToken GetPropertyCaseInsensitive(JObject obj, string name)
        {
            return obj?.Properties()
                .FirstOrDefault(p => String.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }

        private static string GetStringCaseInsensitive(JObject obj, string name)
        {
            var tok = GetPropertyCaseInsensitive(obj, name);
            if (tok == null) return null;

            // Handles null, string, number, etc.
            return tok.Type == JTokenType.Null ? null : tok.ToString();
        }
    }
}