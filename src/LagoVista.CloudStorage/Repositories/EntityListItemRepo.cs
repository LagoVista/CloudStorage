using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Repositories
{
    public class EntityListItemRepo<TEntity> : DocumentDBRepoBase<TEntity>, IEntityListItemRepo where TEntity : class, IEntityBase
    {
        private readonly IAdminLogger _logger;
        private readonly IDocumentStorageClient _storageClient;

        public EntityListItemRepo(string endpoint, string sharedKey, string dbName, IDocumentCloudCachedServices cloudServices) : base(endpoint, sharedKey, dbName, cloudServices)
        {
            if (cloudServices == null) throw new ArgumentNullException(nameof(cloudServices));
            _logger = cloudServices.AdminLogger;
            _storageClient = cloudServices.DocumentStorageClientProvider.GetClient();
        }

        public async Task<ListResponse<EntityListItem>> GetListItemsAsync(string orgId, ListRequest listRequest)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            try
            {
                var sw = Stopwatch.StartNew();
                var items = (await _storageClient.QueryKnownAsync<EntityListItem>(typeof(TEntity).Name, CreateListRequest(DocumentQueryType.EntityListItems, orgId, listRequest))).ToList();
                var response = ListResponse<EntityListItem>.Create(listRequest, items);
                response.Categories = await GetCategoryOptionsAsync(orgId, listRequest);
                response.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));
                response.StatusOptions = GetStatusOptions();

                _logger.AddCustomEvent(LogLevel.Message, $"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetListItemsAsync)}]", $"Returned {items.Count} {typeof(TEntity).Name} list items in {sw.Elapsed.TotalMilliseconds} ms",
                    items.Count.ToString().ToKVP("recordCount"), typeof(TEntity).Name.ToKVP("entityType"), orgId.ToKVP("orgId"));
                return response;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetListItemsAsync)}]", ex, typeof(TEntity).Name.ToKVP("entityType"), orgId.ToKVP("orgId"));
                var response = ListResponse<EntityListItem>.Create(new List<EntityListItem>());
                response.Errors.Add(new ErrorMessage(ex.Message));
                return response;
            }
        }

        public async Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string orgId, ListRequest listRequest)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            try
            {
                var sw = Stopwatch.StartNew();
                var items = (await _storageClient.QueryKnownAsync<EntityHeader>(typeof(TEntity).Name, CreateListRequest(DocumentQueryType.EntityListHeaders, orgId, listRequest))).ToList();
                var response = ListResponse<EntityHeader>.Create(listRequest, items);
                _logger.AddCustomEvent(LogLevel.Message, $"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetEntityHeadersAsync)}]", $"Returned {items.Count} {typeof(TEntity).Name} entity headers in {sw.Elapsed.TotalMilliseconds} ms",
                    items.Count.ToString().ToKVP("recordCount"), typeof(TEntity).Name.ToKVP("entityType"), orgId.ToKVP("orgId"));
                return response;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetEntityHeadersAsync)}]", ex, typeof(TEntity).Name.ToKVP("entityType"), orgId.ToKVP("orgId"));
                var response = ListResponse<EntityHeader>.Create(new List<EntityHeader>());
                response.Errors.Add(new ErrorMessage(ex.Message));
                return response;
            }
        }

        private async Task<List<EnumDescription>> GetCategoryOptionsAsync(string orgId, ListRequest listRequest)
        {
            var categories = await _storageClient.QueryKnownAsync<EntityHeader>(typeof(TEntity).Name, CreateListRequest(DocumentQueryType.EntityListCategories, orgId, listRequest));
            return categories.Where(category => !String.IsNullOrWhiteSpace(category.Key))
                .Select(category => EnumDescription.Create(category.Id, category.Key, category.Text)).ToList();
        }

        private static DocumentQueryRequest CreateListRequest(DocumentQueryType queryType, string orgId, ListRequest listRequest)
        {
            if (listRequest.OrderBy != null && listRequest.OrderByDesc != null)
                throw new InvalidOperationException("OrderBy and OrderByDesc cannot both be provided.");

            var orderBy = listRequest.OrderByDesc ?? listRequest.OrderBy ?? OrderByTypes.Name;
            return new DocumentQueryRequest(queryType)
                .WithParameter("entityType", typeof(TEntity).Name)
                .WithParameter("orgId", orgId)
                .WithParameter("showDeleted", listRequest.ShowDeleted)
                .WithParameter("showDrafts", listRequest.ShowDrafts)
                .WithParameter("categoryKey", listRequest.CategoryKey ?? String.Empty)
                .WithParameter("statusKey", listRequest.StatusKey ?? String.Empty)
                .WithParameter("labelKey", listRequest.LabelKey ?? String.Empty)
                .WithParameter("searchText", String.IsNullOrWhiteSpace(listRequest.SearchText) ? String.Empty : listRequest.SearchText.Trim())
                .WithParameter("orderBy", (int)orderBy)
                .WithParameter("descending", listRequest.OrderByDesc != null)
                .WithParameter("pageIndex", Math.Max(1, listRequest.PageIndex))
                .WithParameter("pageSize", Math.Max(1, listRequest.PageSize));
        }

        private static List<EntityHeader> GetStatusOptions()
        {
            var statusProperty = typeof(TEntity).GetRuntimeProperty("Status");
            if (statusProperty == null) return new List<EntityHeader>();
            var statusType = statusProperty.PropertyType;
            if (!statusType.GetTypeInfo().IsGenericType || statusType.GetGenericTypeDefinition() != typeof(EntityHeader<>)) return new List<EntityHeader>();
            var enumType = statusType.GenericTypeArguments.FirstOrDefault();
            if (enumType == null || !enumType.GetTypeInfo().IsEnum) return new List<EntityHeader>();

            var options = new List<Tuple<int, EntityHeader>>();
            foreach (var enumValue in Enum.GetValues(enumType))
            {
                var enumMember = enumType.GetRuntimeField(enumValue.ToString());
                var enumLabel = enumMember?.GetCustomAttribute<EnumLabelAttribute>();
                if (enumLabel == null || !enumLabel.IsActive) continue;
                var labelProperty = enumLabel.ResourceType.GetRuntimeProperty(enumLabel.LabelResource);
                var label = labelProperty?.GetValue(null) as string;
                if (String.IsNullOrWhiteSpace(label)) label = enumValue.ToString();
                var sortOrder = enumLabel.SortOrder >= 0 ? enumLabel.SortOrder : Convert.ToInt32(enumValue);
                options.Add(new Tuple<int, EntityHeader>(sortOrder, EntityHeader.Create(enumLabel.Key, enumLabel.Key, label)));
            }

            return options.OrderBy(option => option.Item1).ThenBy(option => option.Item2.Text).Select(option => option.Item2).ToList();
        }
    }
}
