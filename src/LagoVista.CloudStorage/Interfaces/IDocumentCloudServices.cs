using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCloudServices 
    {
        IAdminLogger AdminLogger { get; }
        ICosmosClientProvider CosmosClientProvider { get; }
        IDependencyManager DependencyManager { get;  }
        IProducedArtifactService ProducedArtifactService { get; }

        IUserNotificationService UserNotificationService { get; }

        IRagIndexingServices RagIndexingServices { get; }

        IFkIndexTableWriterBatched FkIndexTableWriter { get; }
    }
}
