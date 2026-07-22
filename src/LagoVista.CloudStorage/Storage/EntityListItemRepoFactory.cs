using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class EntityListItemRepoFactory : IEntityListItemRepoFactory
    {
        private readonly IDefaultConnectionSettings _connectionSettings;
        private readonly IDocumentCloudCachedServices _cloudServices;

        public EntityListItemRepoFactory(IDefaultConnectionSettings connectionSettings, IDocumentCloudCachedServices cloudServices)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _cloudServices = cloudServices ?? throw new ArgumentNullException(nameof(cloudServices));
        }

        public IEntityListItemRepo Create(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (!typeof(IEntityBase).IsAssignableFrom(entityType))
                throw new InvalidOperationException($"Entity type '{entityType.FullName}' must implement {nameof(IEntityBase)}.");

            var connectionSettings = _connectionSettings.DefaultDocDbSettings;
            if (connectionSettings == null)
                throw new InvalidOperationException("Default document database connection settings are not available.");

            var repoType = typeof(EntityListItemRepo<>).MakeGenericType(entityType);
            var repo = Activator.CreateInstance(
                repoType,
                connectionSettings.Uri,
                connectionSettings.AccessKey,
                connectionSettings.ResourceName,
                _cloudServices) as IEntityListItemRepo;

            if (repo == null)
                throw new InvalidOperationException($"Could not create an entity list repository for '{entityType.FullName}'.");

            return repo;
        }
    }
}
