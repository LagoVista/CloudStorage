using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ISyncConnectionSettings
    {
        IConnectionSettings SyncConnectionSettings { get; }
    }
}
