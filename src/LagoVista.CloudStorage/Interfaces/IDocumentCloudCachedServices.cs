using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCloudCachedServices : IDocumentCloudServices
    {
        ICacheProvider CacheProvider { get; }
        ICacheAborter CacheAborter { get; }
    }
}
