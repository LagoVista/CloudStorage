using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;

namespace LagoVista.CloudStorage.Repositories
{
    public class EntityListItemRepoFactory : IEntityListItemRepoFactory
    {
        private readonly IDocumentCloudCachedServices _cloudServices;

        public EntityListItemRepoFactory(IDocumentCloudCachedServices cloudServices)
        {
            _cloudServices = cloudServices ?? throw new ArgumentNullException(nameof(cloudServices));
        }

        public IEntityListItemRepo Create(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (!typeof(IEntityBase).IsAssignableFrom(entityType))
                throw new InvalidOperationException($"Entity type '{entityType.FullName}' must implement {nameof(IEntityBase)}.");

            // EntityListItemRepo already obtains the selected provider through the
            // shared document client provider. Do not bypass provider selection with
            // legacy DefaultDocDBStorage connection strings.
            var repoType = typeof(EntityListItemRepo<>).MakeGenericType(entityType);
            var repo = Activator.CreateInstance(repoType, _cloudServices) as IEntityListItemRepo;

            if (repo == null)
                throw new InvalidOperationException($"Could not create an entity list repository for '{entityType.FullName}'.");

            return repo;
        }
    }
}
