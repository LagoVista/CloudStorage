using LagoVista.CloudStorage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentCollectionNameResolver : IDocumentCollectionNameResolver
    {
        public const string EntitiesCollectionName = "Entities";

        public string Resolve(string databaseName, Type entityType, string explicitCollectionName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (!String.IsNullOrWhiteSpace(explicitCollectionName)) return Normalize(explicitCollectionName);
            return EntitiesCollectionName;
        }

        public bool TryResolve(string databaseName, string entityTypeName, out string collectionName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            collectionName = EntitiesCollectionName;

            if (String.IsNullOrWhiteSpace(entityTypeName)) return false;

            return GetLoadedEntityTypes()
                .Any(type => String.Equals(type.Name, entityTypeName, StringComparison.OrdinalIgnoreCase));
        }

        public string GetFallback(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            return EntitiesCollectionName;
        }

        private static IEnumerable<Type> GetLoadedEntityTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetTypes(assembly)) yield return type;
            }
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        private static string Normalize(string collectionName)
        {
            var normalized = collectionName.Trim().Replace('\0', '_').Replace('$', '_');
            if (normalized.StartsWith("system.", StringComparison.OrdinalIgnoreCase)) normalized = "_" + normalized;
            if (String.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException("Document collection name resolved to an empty value.");
            return normalized;
        }
    }
}
