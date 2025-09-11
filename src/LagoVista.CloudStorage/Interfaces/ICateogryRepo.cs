using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;


namespace LagoVista.CloudStorage
{
    public interface ICategoryRepo
    {
        Task AddCategoryAsync(Category category);
        Task DeleteCategoryAsync(string id);
        Task<Category> GetCategoryAsync(string id);
        Task<ListResponse<Category>> GetCategoriesAsync(string orgId, string categoryType, ListRequest listRequest);
        Task UpdateCategoryAsync(Category category);
    }
}
