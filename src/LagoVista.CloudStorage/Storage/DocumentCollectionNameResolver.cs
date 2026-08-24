using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentCollectionNameResolver : IDocumentCollectionNameResolver
    {
        public const string SharedEntitiesCollectionName = "SharedEntities";
        public const string OrganizationEntitiesCollectionName = "OrganizationEntities";

        public string Resolve(string databaseName, Type entityType, string explicitCollectionName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (!String.IsNullOrWhiteSpace(explicitCollectionName)) return Normalize(explicitCollectionName);
            if (entityType == null) return GetFallback(databaseName);

            var isShareable = entityType.GetCustomAttribute<ShareableStorageAttribute>(true) != null;
            var isDedicated = entityType.GetCustomAttribute<DedicatedStorageCollectionAttribute>(true) != null;

            if (isShareable && isDedicated)
                throw new InvalidOperationException($"Entity type '{entityType.FullName}' cannot use both ShareableStorageAttribute and DedicatedStorageCollectionAttribute.");

            if (isShareable) return SharedEntitiesCollectionName;
            if (isDedicated) return Normalize(entityType.Name);
            return OrganizationEntitiesCollectionName;
        }

        public bool TryResolve(string databaseName, string entityTypeName, out string collectionName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(entityTypeName))
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            var matches = GetLoadedEntityTypes()
                .Where(type => String.Equals(type.Name, entityTypeName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            var collections = matches
                .Select(type => Resolve(databaseName, type))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (collections.Count != 1)
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            collectionName = collections[0];
            return true;
        }

        public string GetFallback(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            return OrganizationEntitiesCollectionName;
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
