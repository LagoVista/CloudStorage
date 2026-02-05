using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.StorageProviders
{
    public class DocumentCloudServices : IDocumentCloudServices
    {
        public DocumentCloudServices(IAdminLogger adminLogger, IFkIndexTableWriterBatched fkIndexTableWriter, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices)
        {
            AdminLogger = adminLogger;
            DependencyManager = dependencyManager;
            UserNotificationService = userNotificationService;
            RagIndexingServices = ragServices;
            FkIndexTableWriter = fkIndexTableWriter;
        }

        public IAdminLogger AdminLogger { get; }

        public IDependencyManager DependencyManager { get; }

        public IUserNotificationService UserNotificationService { get; }

        public IRagIndexingServices RagIndexingServices { get; }

        public IFkIndexTableWriterBatched FkIndexTableWriter { get; }
    }

    public class DocumentCloudCachedServices : DocumentCloudServices, IDocumentCloudCachedServices
    {
        public DocumentCloudCachedServices(IAdminLogger adminLogger, IFkIndexTableWriterBatched fkIndexTableWriter, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheAborter aborter, ICacheProvider cacheProvider)
            : base(adminLogger, fkIndexTableWriter, dependencyManager, userNotificationService, ragServices)
        {
            CacheProvider = cacheProvider;
            CacheAborter = aborter;
        }
        public ICacheProvider CacheProvider { get; }

        public ICacheAborter CacheAborter { get; }
    }
}
