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
        public string Resolve(string databaseName, Type entityType, string explicitCollectionName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (!String.IsNullOrWhiteSpace(explicitCollectionName)) return Normalize(explicitCollectionName);
            if (entityType == null) return GetFallback(databaseName);

            var attribute = entityType.GetCustomAttribute<EntityDescriptionAttribute>(true);
            if (attribute == null || String.IsNullOrWhiteSpace(attribute.Domain)) return GetFallback(databaseName);
            return Normalize(attribute.Domain);
        }

        public bool TryResolve(string databaseName, string entityTypeName, out string collectionName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (String.IsNullOrWhiteSpace(entityTypeName))
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            var matches = GetLoadedEntityTypes().Where(type => String.Equals(type.Name, entityTypeName, StringComparison.OrdinalIgnoreCase)).Select(type => new { Type = type, Attribute = type.GetCustomAttribute<EntityDescriptionAttribute>(true) }).Where(item => item.Attribute != null && !String.IsNullOrWhiteSpace(item.Attribute.Domain)).ToList();
            if (matches.Count == 0)
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            var domains = matches.Select(item => Normalize(item.Attribute.Domain)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (domains.Count != 1)
            {
                collectionName = GetFallback(databaseName);
                return false;
            }

            collectionName = domains[0];
            return true;
        }

        public string GetFallback(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            return $"{databaseName}_Collections";
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
