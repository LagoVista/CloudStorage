using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public enum PatchOp { Set, Remove, Add }

    public class PatchStep
    {
        public PatchOp Op { get; set; }
        public string CosmosPath { get; set; }
        public JToken Value { get; set; }
        public string LogicalPath { get; set; }

        public string ValuePreview => Value == null ? "(removed)"
            : Value.Type == JTokenType.Object || Value.Type == JTokenType.Array
                ? Value.ToString(Formatting.None).Truncate(120)
                : Value.ToString();

        public string ValuePretty => Value?.ToString(Formatting.Indented) ?? "(removed)";

    }

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();

            if (value.Length <= maxLength)
                return value;

            var truncated = value.Substring(0, maxLength);

            // cut at last safe boundary if possible
            var lastComma = truncated.LastIndexOf(',');
            var lastSpace = truncated.LastIndexOf(' ');
            var cut = Math.Max(lastComma, lastSpace);

            if (cut > maxLength * 0.6)
                truncated = truncated.Substring(0, cut);

            return truncated + "...";
        }
    }

}

