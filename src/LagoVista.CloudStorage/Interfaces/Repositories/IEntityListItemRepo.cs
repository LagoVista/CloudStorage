using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListItemRepo
    {
        Task<ListResponse<EntityListItem>> GetListItemsAsync(string orgId, ListRequest listRequest);
        Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string orgId, ListRequest listRequest);
    }
}
