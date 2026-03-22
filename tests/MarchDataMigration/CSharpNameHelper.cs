// ================================
// File: LegacyMigrationScaffolding/Generation/CSharpNameHelper.cs
// ================================
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LegacyMigrationScaffolding.Generation
{
    public static class CSharpNameHelper
    {
        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var parts = input
                .Replace("-", "_")
                .Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;

                var textInfo = CultureInfo.InvariantCulture.TextInfo;
                var piece = textInfo.ToTitleCase(part.ToLowerInvariant());
                builder.Append(piece);
            }

            var result = builder.ToString();

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        public static string EscapeStringLiteral(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static string SafeIdentifier(string input)
        {
            var value = ToPascalCase(input);

            if (new[] { "namespace", "class", "event", "string", "int", "long", "decimal", "public", "private", "protected", "internal", "base", "new" }.Contains(value.ToLowerInvariant()))
                return "@" + value;

            return value;
        }
    }
}