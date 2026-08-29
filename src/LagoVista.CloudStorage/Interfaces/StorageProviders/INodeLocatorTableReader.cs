using LagoVista.CloudStorage.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface INodeLocatorTableReader
    {
        Task<NodeLocatorEntry> TryGetAsync(string nodeId, CancellationToken ct = default);
    }
}
