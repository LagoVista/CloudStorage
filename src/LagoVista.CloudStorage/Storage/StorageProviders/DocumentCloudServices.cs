using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;

namespace LagoVista.CloudStorage.StorageProviders
{
    public class DocumentCloudServices : IDocumentCloudServices
    {
        public DocumentCloudServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices)
            : this(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, null)
        {
        }

        public DocumentCloudServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, IDocumentStorageClientProvider documentStorageClientProvider)
        {
            AdminLogger = adminLogger;
            CosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            DocumentStorageClientProvider = documentStorageClientProvider;
            DependencyManager = dependencyManager;
            UserNotificationService = userNotificationService;
            RagIndexingServices = ragServices;
            FkIndexTableWriter = fkIndexTableWriter;
            ProducedArtifactService = producedArtifactService;
        }

        public IAdminLogger AdminLogger { get; }

        public ICosmosClientProvider CosmosClientProvider { get; }

        public IDocumentStorageClientProvider DocumentStorageClientProvider { get; }

        public IDependencyManager DependencyManager { get; }

        public IUserNotificationService UserNotificationService { get; }

        public IRagIndexingServices RagIndexingServices { get; }

        public IFkIndexTableWriterBatched FkIndexTableWriter { get; }

        public IProducedArtifactService ProducedArtifactService { get; }
    }

    public class DocumentCloudCachedServices : DocumentCloudServices, IDocumentCloudCachedServices
    {
        public DocumentCloudCachedServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider, IEntityListCacheInvalidator entityListCacheInvalidator)
            : this(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, aborter, cacheProvider, entityListCacheInvalidator, null)
        {
        }

        public DocumentCloudCachedServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider, IEntityListCacheInvalidator entityListCacheInvalidator, IDocumentStorageClientProvider documentStorageClientProvider)
            : base(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices, documentStorageClientProvider)
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
