using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;

namespace LagoVista.CloudStorage.Storage.StorageProviders
{
    public class DocumentCloudServices : IDocumentCloudServices
    {
        public DocumentCloudServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ISystemUsers systemUsers)
            : this(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, null, systemUsers)
        {
        }

        public DocumentCloudServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, IDocumentStorageClientProvider documentStorageClientProvider, ISystemUsers systemUsers)
        {
            AdminLogger = adminLogger;
            CosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            DocumentStorageClientProvider = documentStorageClientProvider;
            DependencyManager = dependencyManager;
            UserNotificationService = userNotificationService;
            RagIndexingServices = ragServices;
            FkIndexTableWriter = fkIndexTableWriter;
            ProducedArtifactService = producedArtifactService;
            SystemUsers = systemUsers;
        }

        public IAdminLogger AdminLogger { get; }

        public ICosmosClientProvider CosmosClientProvider { get; }

        public IDocumentStorageClientProvider DocumentStorageClientProvider { get; }

        public IDependencyManager DependencyManager { get; }

        public IUserNotificationService UserNotificationService { get; }

        public IRagIndexingServices RagIndexingServices { get; }

        public IFkIndexTableWriterBatched FkIndexTableWriter { get; }

        public ISystemUsers SystemUsers { get; }
        public IProducedArtifactService ProducedArtifactService { get; }
    }

    public class DocumentCloudCachedServices : DocumentCloudServices, IDocumentCloudCachedServices
    {
        public DocumentCloudCachedServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, ISystemUsers systemUsers, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider, IEntityListCacheInvalidator entityListCacheInvalidator)
            : this(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, aborter, cacheProvider, entityListCacheInvalidator, systemUsers, null)
        {
        }

        public DocumentCloudCachedServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider, IEntityListCacheInvalidator entityListCacheInvalidator, ISystemUsers systemUsers, IDocumentStorageClientProvider documentStorageClientProvider)
            : base(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, documentStorageClientProvider, systemUsers)
        {
            CacheProvider = cacheProvider;
            CacheAborter = aborter;
            EntityListCacheInvalidator = entityListCacheInvalidator ?? throw new ArgumentNullException(nameof(entityListCacheInvalidator));
        }
        public ICacheProvider CacheProvider { get; }

        public ICacheAborter CacheAborter { get; }

        public IEntityListCacheInvalidator EntityListCacheInvalidator { get; }
    }
}
