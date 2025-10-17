// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: f1be5bef1262738e3d955379cb34ee88a11a9cfed225ebc6c5ab9b3745ad422d
// IndexVersion: 1
// --- END CODE INDEX META ---
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
