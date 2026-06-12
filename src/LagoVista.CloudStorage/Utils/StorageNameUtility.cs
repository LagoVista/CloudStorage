using System;
using System.Linq;
using System.Text;

namespace LagoVista.CloudStorage.Utils
{
    internal static class StorageNameUtility
    {
        public static string ToBlobContainerName(string typeName)
        {
            if (String.IsNullOrWhiteSpace(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var name = ToKebabCase(typeName);

            if (!name.EndsWith("s"))
                name += "s";

            return name.ToLowerInvariant();
        }

        public static string ToTableName(string typeName)
        {
            if (String.IsNullOrWhiteSpace(typeName))
                throw new ArgumentNullException(nameof(typeName));

            var chars = typeName.Where(Char.IsLetterOrDigit).ToArray();
            var name = new string(chars);

            if (String.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Could not create table name from type name [{typeName}].");

            if (!Char.IsLetter(name[0]))
                name = "T" + name;

            return name;
        }

        private static string ToKebabCase(string value)
        {
            var result = new StringBuilder();

            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];

                if (Char.IsUpper(current) && index > 0)
                    result.Append("-");

                if (Char.IsLetterOrDigit(current))
                    result.Append(Char.ToLowerInvariant(current));
                else if (current == '-' || current == '_')
                    result.Append("-");
            }

            return result.ToString().Trim('-');
        }
    }
}
