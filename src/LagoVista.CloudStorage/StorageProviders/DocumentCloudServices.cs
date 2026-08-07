using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.StorageProviders
{
    public class DocumentCloudServices : IDocumentCloudServices
    {
        public DocumentCloudServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices)
        {
            AdminLogger = adminLogger;
            CosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            DependencyManager = dependencyManager;
            UserNotificationService = userNotificationService;
            RagIndexingServices = ragServices;
            FkIndexTableWriter = fkIndexTableWriter;
            ProducedArtifactService = producedArtifactService;
        }

        public IAdminLogger AdminLogger { get; }

        public ICosmosClientProvider CosmosClientProvider { get; }

        public IDependencyManager DependencyManager { get; }

        public IUserNotificationService UserNotificationService { get; }

        public IRagIndexingServices RagIndexingServices { get; }

        public IFkIndexTableWriterBatched FkIndexTableWriter { get; }

        public IProducedArtifactService ProducedArtifactService { get; }
    }

    public class DocumentCloudCachedServices : DocumentCloudServices, IDocumentCloudCachedServices
    {
        public DocumentCloudCachedServices(IAdminLogger adminLogger, ICosmosClientProvider cosmosClientProvider, IFkIndexTableWriterBatched fkIndexTableWriter, IProducedArtifactService producedArtifactService, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider, IEntityListCacheInvalidator entityListCacheInvalidator)
            : base(adminLogger, cosmosClientProvider, fkIndexTableWriter, producedArtifactService, dependencyManager, userNotificationService, ragServices)
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
