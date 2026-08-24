using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using System;
using System.Reflection;

namespace LagoVista.CloudStorage.DocumentDB
{
    public static class EntityDocumentStoragePolicy
    {
        public const string CosmosPartitionKeyPath = "/OwnerOrganization/Id";

        public static bool IsShareable(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            return entityType.GetCustomAttribute<ShareableStorageAttribute>(true) != null;
        }

        public static bool IsDedicated(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            return entityType.GetCustomAttribute<DedicatedStorageCollectionAttribute>(true) != null;
        }

        public static void ValidateForWrite<TEntity>(TEntity entity) where TEntity : class, IEntityBase
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (entity.OwnerOrganization == null || String.IsNullOrWhiteSpace(entity.OwnerOrganization.Id))
                throw new InvalidOperationException($"{typeof(TEntity).Name} requires OwnerOrganization.Id for document storage.");

            if (!IsShareable(typeof(TEntity)) && entity.IsPublic)
                throw new InvalidOperationException($"{typeof(TEntity).Name} cannot be public because it is not marked with ShareableStorageAttribute.");
        }
    }
}
