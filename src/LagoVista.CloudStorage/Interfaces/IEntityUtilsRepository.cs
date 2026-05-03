using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityUtilsRepository
    {
        Task<InvokeResult> IndexEntityAsync(string id, EntityHeader org, EntityHeader user, CancellationToken ct);
        Task<InvokeResult> CalculateHashAsync(string id, CancellationToken ct);
    }
}
