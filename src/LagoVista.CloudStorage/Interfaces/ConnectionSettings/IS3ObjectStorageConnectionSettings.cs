using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces.ConnectionSettings
{
    public interface IS3ObjectStorageConnectionSettings
    {
        string Host { get; }
        int Port { get; }
        string AccessKey { get; }
        string SecretKey { get; }
        bool UseTls { get; }
        string Region { get; }

        string PublicHost { get; }
        int PublicPort { get; }
        bool PublicUseTls { get; }
    }
}
