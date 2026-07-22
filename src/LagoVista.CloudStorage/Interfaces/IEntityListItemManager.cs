using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListItemManager
    {
        Task<ListResponse<EntityListItem>> GetListItemsAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest);
    }
}
