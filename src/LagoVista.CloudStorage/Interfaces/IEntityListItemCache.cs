using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListItemCache
    {
        Task<ListResponse<EntityListItem>> GetListItemsAsync(string orgId, string entityType, ListRequest request);
        Task SetListItemsAsync(string orgId, string entityType, ListRequest request, ListResponse<EntityListItem> response);

        Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string orgId, string entityType, ListRequest request);
        Task SetEntityHeadersAsync(string orgId, string entityType, ListRequest request, ListResponse<EntityHeader> response);
    }
}
