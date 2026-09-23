using LagoVista.CloudStorage.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IValkeyAdminRepo
    {
        Task<IReadOnlyList<ValkeyKeyInfo>> ScanAsync(ValkeyScanRequest request, CancellationToken cancellationToken = default);
        Task<ValkeyKeyInfo> GetAsync(int database, string key, CancellationToken cancellationToken = default);
        Task SetAsync(ValkeySetRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int database, string key, CancellationToken cancellationToken = default);
    }
}
