using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDefaultConnectionSettings
    {
        IConnectionSettings DefaultDocDbSettings { get; }
        IConnectionSettings DefaultTableStorageSettings { get; }
        IConnectionSettings EHCheckPointStorageSettings { get; }

    }
}
