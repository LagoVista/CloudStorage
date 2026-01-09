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
        public DocumentCloudServices(IAdminLogger adminLogger, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices)
        {
            AdminLogger = adminLogger;
            DependencyManager = dependencyManager;
            UserNotificationService = userNotificationService;
            RagIndexingServices = ragServices;
        }

        public IAdminLogger AdminLogger { get; }

        public IDependencyManager DependencyManager { get; }

        public IUserNotificationService UserNotificationService { get; }

        public IRagIndexingServices RagIndexingServices { get; }
    }

    public class DocumentCloudCachedServices : DocumentCloudServices, IDocumentCloudCachedServices
    {
        public DocumentCloudCachedServices(IAdminLogger adminLogger, IDependencyManager dependencyManager, IUserNotificationService userNotificationService, IRagIndexingServices ragServices, ICacheProvider cacheProvider)
            : base(adminLogger, dependencyManager, userNotificationService, ragServices)
        {
            CacheProvider = cacheProvider;
        }
        public ICacheProvider CacheProvider { get; }
    }
}
