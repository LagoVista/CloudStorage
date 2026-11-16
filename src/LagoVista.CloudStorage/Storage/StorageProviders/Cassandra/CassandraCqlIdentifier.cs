using System;
using System.Linq;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    /// <summary>
    /// Quoting CQL identifiers keeps generated snake_case names compatible with
    /// existing Cassandra tables while allowing column names such as create,
    /// read, update and delete. Always quote every identifier in emitted CQL;
    /// raw column names remain unchanged for driver row/metadata access.
    /// </summary>
    internal static class CassandraCqlIdentifier
    {
        public static string Quote(string name)
        {
            if (String.IsNullOrWhiteSpace(name) ||
                !Char.IsLetter(name[0]) ||
                name.Any(character => !(Char.IsLetterOrDigit(character) || character == '_')))
                throw new ArgumentException("Invalid Cassandra identifier.", nameof(name));

            return "\"" + name.ToLowerInvariant() + "\"";
        }

        public static bool TargetMatches(string metadataTarget, string columnName)
        {
            // Cassandra SAI versions may expose either a bare target or a quoted target.
            if (String.IsNullOrWhiteSpace(metadataTarget))
                return false;

            return String.Equals(metadataTarget.Trim('"'), columnName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
