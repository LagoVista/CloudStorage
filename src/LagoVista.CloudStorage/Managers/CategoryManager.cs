// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 3c3bf65b51b3675248f8fd024c0e3b84ec2a7cf09cf3a7af7b8b12306a185b3a
// IndexVersion: 1
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Managers
{
    public class CategoryManager : ManagerBase, ICategoryManager
    {
        readonly ICategoryRepo _categoryRepo;

        public CategoryManager(ICategoryRepo categoryRepo, IAdminLogger logger, IAppConfig appConfig, IDependencyManager dependencyManager, ISecurity security)
            : base(logger, appConfig, dependencyManager, security)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<InvokeResult> AddCategoryAsync(Category category, EntityHeader org, EntityHeader user)
        {
            ValidationCheck(category, Actions.Create);
            await AuthorizeAsync(category, AuthorizeResult.AuthorizeActions.Create, user, org);
            await _categoryRepo.AddCategoryAsync(category);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteCategoryAsync(string id, EntityHeader org, EntityHeader user)
        {
            var Competitoc = await _categoryRepo.GetCategoryAsync(id);
            await AuthorizeAsync(Competitoc, AuthorizeResult.AuthorizeActions.Delete, user, org);

            await _categoryRepo.DeleteCategoryAsync(id);
            return InvokeResult.Success;
        }

        public Task<Category> GetCategoryAsync(string id, EntityHeader org, EntityHeader user)
        {
            return _categoryRepo.GetCategoryAsync(id);
        }

        public async Task<ListResponse<Category>> GetCategoriesAsync(string categoryType, ListRequest listRequest, EntityHeader org, EntityHeader user)
        {
            return await _categoryRepo.GetCategoriesAsync(org.Id, categoryType, listRequest);
        }

        public async Task<InvokeResult> UpdateCategoryAsync(Category category, EntityHeader org, EntityHeader user)
        {
            ValidationCheck(category, Actions.Create);
            await AuthorizeAsync(category, AuthorizeResult.AuthorizeActions.Update, user, org);
            await _categoryRepo.UpdateCategoryAsync(category);
            return InvokeResult.Success;
        }
    }
}
