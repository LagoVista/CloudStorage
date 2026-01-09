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
        IDependencyManager DependencyManager { get;  }

        IUserNotificationService UserNotificationService { get; }

        IRagServices RagServices { get; }
    }
}
