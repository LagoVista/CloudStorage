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
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public class EntityListItemRepo<TEntity> : DocumentDBRepoBase<TEntity>, IEntityListItemRepo where TEntity : class, IEntityBase
    {
        private readonly IAdminLogger _logger;

        public EntityListItemRepo(string endpoint, string sharedKey, string dbName, IDocumentCloudCachedServices cloudServices) : base(endpoint, sharedKey, dbName, cloudServices)
        {
            _logger = cloudServices.AdminLogger;
        }

        public async Task<ListResponse<EntityListItem>> GetListItemsAsync(string orgId, ListRequest listRequest)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));

            try
            {
                var sw = Stopwatch.StartNew();
                var query = CreateQueryDefinition(BuildListItemSql(listRequest), orgId, listRequest);
                var items = await ExecuteAsync<EntityListItem>(query, listRequest.PageSize);
                var response = ListResponse<EntityListItem>.Create(listRequest, items);
                response.Categories = await GetCategoryOptionsAsync(orgId, listRequest);
                response.Categories.Insert(0, EnumDescription.CreateSelect("-select category-"));
                response.StatusOptions = GetStatusOptions();

                _logger.AddCustomEvent(LogLevel.Message, $"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetListItemsAsync)}]", $"Returned {items.Count} {typeof(TEntity).Name} list items in {sw.Elapsed.TotalMilliseconds} ms",
                    items.Count.ToString().ToKVP("recordCount"),
                    typeof(TEntity).Name.ToKVP("entityType"),
                    orgId.ToKVP("orgId"));

                return response;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetListItemsAsync)}]", ex,
                    typeof(TEntity).Name.ToKVP("entityType"),
                    orgId.ToKVP("orgId"));

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
                var query = CreateQueryDefinition(BuildEntityHeaderSql(listRequest), orgId, listRequest);
                var items = await ExecuteAsync<EntityHeader>(query, listRequest.PageSize);
                var response = ListResponse<EntityHeader>.Create(listRequest, items);

                _logger.AddCustomEvent(LogLevel.Message, $"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetEntityHeadersAsync)}]", $"Returned {items.Count} {typeof(TEntity).Name} entity headers in {sw.Elapsed.TotalMilliseconds} ms",
                    items.Count.ToString().ToKVP("recordCount"),
                    typeof(TEntity).Name.ToKVP("entityType"),
                    orgId.ToKVP("orgId"));

                return response;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[{nameof(EntityListItemRepo<TEntity>)}__{nameof(GetEntityHeadersAsync)}]", ex,
                    typeof(TEntity).Name.ToKVP("entityType"),
                    orgId.ToKVP("orgId"));

                var response = ListResponse<EntityHeader>.Create(new List<EntityHeader>());
                response.Errors.Add(new ErrorMessage(ex.Message));
                return response;
            }
        }

        private async Task<List<EnumDescription>> GetCategoryOptionsAsync(string orgId, ListRequest listRequest)
        {
            var sql = @"SELECT DISTINCT VALUE {
    ""Id"": c.Category.Id,
    ""Key"": c.Category.Key,
    ""Text"": c.Category.Text
}
FROM c
WHERE c.EntityType = @entityType
  AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)
  AND IS_DEFINED(c.Category)
  AND IS_DEFINED(c.Category.Key)";

            if (!listRequest.ShowDeleted)
                sql += " AND (NOT IS_DEFINED(c.IsDeleted) OR c.IsDeleted = false)";

            if (!listRequest.ShowDrafts)
                sql += " AND (NOT IS_DEFINED(c.IsDraft) OR c.IsDraft = false)";

            sql += " ORDER BY c.Category.Text";

            var query = new QueryDefinition(sql)
                .WithParameter("@entityType", typeof(TEntity).Name)
                .WithParameter("@orgId", orgId);

            var categories = await ExecuteAsync<EntityHeader>(query, 100);
            return categories
                .Where(category => !String.IsNullOrWhiteSpace(category.Key))
                .Select(category => EnumDescription.Create(category.Id, category.Key, category.Text))
                .ToList();
        }

        private static QueryDefinition CreateQueryDefinition(string sql, string orgId, ListRequest listRequest)
        {
            var query = new QueryDefinition(sql)
                .WithParameter("@entityType", typeof(TEntity).Name)
                .WithParameter("@orgId", orgId);

            if (!String.IsNullOrWhiteSpace(listRequest.CategoryKey))
                query.WithParameter("@categoryKey", listRequest.CategoryKey);

            if (!String.IsNullOrWhiteSpace(listRequest.StatusKey))
                query.WithParameter("@statusKey", listRequest.StatusKey);

            if (!String.IsNullOrWhiteSpace(listRequest.LabelKey))
                query.WithParameter("@labelKey", listRequest.LabelKey);

            if (!String.IsNullOrWhiteSpace(listRequest.SearchText))
                query.WithParameter("@searchText", listRequest.SearchText.Trim());

            return query;
        }

        private static List<EntityHeader> GetStatusOptions()
        {
            var statusProperty = typeof(TEntity).GetRuntimeProperty("Status");
            if (statusProperty == null)
                return new List<EntityHeader>();

            var statusType = statusProperty.PropertyType;
            if (!statusType.GetTypeInfo().IsGenericType || statusType.GetGenericTypeDefinition() != typeof(EntityHeader<>))
                return new List<EntityHeader>();

            var enumType = statusType.GenericTypeArguments.FirstOrDefault();
            if (enumType == null || !enumType.GetTypeInfo().IsEnum)
                return new List<EntityHeader>();

            var options = new List<Tuple<int, EntityHeader>>();
            foreach (var enumValue in Enum.GetValues(enumType))
            {
                var enumMember = enumType.GetRuntimeField(enumValue.ToString());
                var enumLabel = enumMember?.GetCustomAttribute<EnumLabelAttribute>();
                if (enumLabel == null || !enumLabel.IsActive)
                    continue;

                var labelProperty = enumLabel.ResourceType.GetRuntimeProperty(enumLabel.LabelResource);
                var label = labelProperty?.GetValue(null) as string;
                if (String.IsNullOrWhiteSpace(label))
                    label = enumValue.ToString();

                var sortOrder = enumLabel.SortOrder >= 0 ? enumLabel.SortOrder : Convert.ToInt32(enumValue);
                options.Add(new Tuple<int, EntityHeader>(sortOrder, EntityHeader.Create(enumLabel.Key, enumLabel.Key, label)));
            }

            return options
                .OrderBy(option => option.Item1)
                .ThenBy(option => option.Item2.Text)
                .Select(option => option.Item2)
                .ToList();
        }

        private async Task<List<TProjection>> ExecuteAsync<TProjection>(QueryDefinition query, int pageSize)
        {
            var items = new List<TProjection>();
            var container = await GetContainerAsync();
            var options = new QueryRequestOptions
            {
                MaxItemCount = Math.Max(1, pageSize)
            };

            using (var iterator = container.GetItemQueryIterator<TProjection>(query, requestOptions: options))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    items.AddRange(response.Resource);
                }
            }

            return items;
        }

        private static string BuildListItemSql(ListRequest request)
        {
            var sql = @"SELECT VALUE {
    ""id"": c.id,
    ""Icon"": c.Icon,
    ""Name"": c.Name,
    ""Key"": c.Key,
    ""IsPublic"": c.IsPublic,
    ""IsDraft"": c.IsDraft,
    ""IsDeleted"": c.IsDeleted,
    ""Category"": c.Category.Text,
    ""Stars"": c.Stars,
    ""RatingsCount"": c.RatingsCount,
    ""Labels"": c.Labels,
    ""Status"": c.Status
}
FROM c
WHERE c.EntityType = @entityType
  AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)";

            sql += BuildCommonWhereClause(request);
            sql += BuildOrderByClause(request);
            sql += BuildPagingClause(request);
            return sql;
        }

        private static string BuildEntityHeaderSql(ListRequest request)
        {
            var sql = @"SELECT VALUE {
    ""Id"": c.id,
    ""Key"": c.Key,
    ""Text"": c.Name
}
FROM c
WHERE c.EntityType = @entityType
  AND (c.IsPublic = true OR c.OwnerOrganization.Id = @orgId)";

            sql += BuildCommonWhereClause(request);
            sql += BuildOrderByClause(request);
            sql += BuildPagingClause(request);
            return sql;
        }

        private static string BuildCommonWhereClause(ListRequest request)
        {
            var sql = String.Empty;

            if (!request.ShowDeleted)
                sql += " AND (NOT IS_DEFINED(c.IsDeleted) OR c.IsDeleted = false)";

            if (!request.ShowDrafts)
                sql += " AND (NOT IS_DEFINED(c.IsDraft) OR c.IsDraft = false)";

            if (!String.IsNullOrWhiteSpace(request.CategoryKey))
                sql += " AND c.Category.Key = @categoryKey";

            if (!String.IsNullOrWhiteSpace(request.StatusKey))
                sql += " AND c.Status.Key = @statusKey";

            if (!String.IsNullOrWhiteSpace(request.LabelKey))
                sql += " AND ARRAY_CONTAINS(c.Labels, {\"Key\": @labelKey}, true)";

            if (!String.IsNullOrWhiteSpace(request.SearchText))
                sql += " AND CONTAINS(c.Name, @searchText, true)";

            return sql;
        }

        private static string BuildOrderByClause(ListRequest request)
        {
            if (request.OrderBy != null && request.OrderByDesc != null)
                throw new InvalidOperationException("OrderBy and OrderByDesc cannot both be provided.");

            if (request.OrderByDesc != null)
                return $" ORDER BY {GetOrderByField(request.OrderByDesc.Value)} DESC";

            if (request.OrderBy != null)
                return $" ORDER BY {GetOrderByField(request.OrderBy.Value)}";

            return " ORDER BY c.Name";
        }

        private static string GetOrderByField(OrderByTypes orderBy)
        {
            switch (orderBy)
            {
                case OrderByTypes.Rating:
                    return "c.Stars";
                case OrderByTypes.CreationDate:
                    return "c.CreationDate";
                case OrderByTypes.LastUpdateDate:
                    return "c.LastUpdatedDate";
                case OrderByTypes.Name:
                default:
                    return "c.Name";
            }
        }

        private static string BuildPagingClause(ListRequest request)
        {
            var pageIndex = Math.Max(1, request.PageIndex);
            var pageSize = Math.Max(1, request.PageSize);
            var offset = (pageIndex - 1) * pageSize;
            return $" OFFSET {offset} LIMIT {pageSize}";
        }
    }
}
