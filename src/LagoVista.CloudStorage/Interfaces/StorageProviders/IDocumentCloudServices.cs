using LagoVista.Core.AI.Interfaces;
using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCloudServices 
    {
        IAdminLogger AdminLogger { get; }
        IRuntimeControlService RuntimeControls { get; }
        ICosmosClientProvider CosmosClientProvider { get; }
        IDocumentStorageClientProvider DocumentStorageClientProvider { get; }
        IDependencyManager DependencyManager { get;  }
        IProducedArtifactService ProducedArtifactService { get; }

        IUserNotificationService UserNotificationService { get; }

        IRagIndexingServices RagIndexingServices { get; }

        IFkIndexTableWriterBatched FkIndexTableWriter { get; }

        ISystemUsers SystemUsers { get; }
    }
}
