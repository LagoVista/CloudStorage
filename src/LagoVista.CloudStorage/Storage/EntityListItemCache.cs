using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public class EntityListItemCache : IEntityListItemCache, IEntityListCacheInvalidator
    {
        private const string GenerationKeyPrefix = "entity-list-generation";
        private const string ListItemsKeyPrefix = "entity-list-items";
        private const string EntityHeadersKeyPrefix = "entity-headers";

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly ICacheProvider _cacheProvider;

        public EntityListItemCache(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        }

        public async Task<ListResponse<EntityListItem>> GetListItemsAsync(string orgId, string entityType, ListRequest request)
        {
            var key = await GetCacheKeyAsync(ListItemsKeyPrefix, orgId, entityType, request);
            return await _cacheProvider.GetAsync<ListResponse<EntityListItem>>(key);
        }

        public async Task SetListItemsAsync(string orgId, string entityType, ListRequest request, ListResponse<EntityListItem> response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var key = await GetCacheKeyAsync(ListItemsKeyPrefix, orgId, entityType, request);
            await _cacheProvider.AddAsync(key, response, CacheDuration);
        }

        public async Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string orgId, string entityType, ListRequest request)
        {
            var key = await GetCacheKeyAsync(EntityHeadersKeyPrefix, orgId, entityType, request);
            return await _cacheProvider.GetAsync<ListResponse<EntityHeader>>(key);
        }

        public async Task SetEntityHeadersAsync(string orgId, string entityType, ListRequest request, ListResponse<EntityHeader> response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var key = await GetCacheKeyAsync(EntityHeadersKeyPrefix, orgId, entityType, request);
            await _cacheProvider.AddAsync(key, response, CacheDuration);
        }

        public Task InvalidateAsync<TEntity>(string orgId)
        {
            return InvalidateAsync(orgId, typeof(TEntity));
        }

        public Task InvalidateAsync(string orgId, Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            return InvalidateAsync(orgId, entityType.Name);
        }

        public async Task InvalidateAsync(string orgId, string entityType)
        {
            var generationKey = GetGenerationKey(orgId, entityType);
            await _cacheProvider.IncrementAsync(generationKey);
        }

        private async Task<string> GetCacheKeyAsync(string prefix, string orgId, string entityType, ListRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var normalizedOrgId = NormalizeRequired(orgId, nameof(orgId));
            var normalizedEntityType = NormalizeRequired(entityType, nameof(entityType));
            var generation = await _cacheProvider.GetLongAsync(GetGenerationKey(normalizedOrgId, normalizedEntityType));
            var requestHash = GetRequestHash(request);

            return $"{prefix}:{normalizedOrgId}:{normalizedEntityType}:{generation}:{requestHash}";
        }

        private static string GetGenerationKey(string orgId, string entityType)
        {
            var normalizedOrgId = NormalizeRequired(orgId, nameof(orgId));
            var normalizedEntityType = NormalizeRequired(entityType, nameof(entityType));

            return $"{GenerationKeyPrefix}:{normalizedOrgId}:{normalizedEntityType}";
        }

        private static string GetRequestHash(ListRequest request)
        {
            var canonicalRequest = String.Join("|", new[]
            {
                NormalizeOptional(request.NextPartitionKey),
                NormalizeOptional(request.NextRowKey),
                request.PageIndex.ToString(CultureInfo.InvariantCulture),
                request.PageSize.ToString(CultureInfo.InvariantCulture),
                NormalizeOptional(request.StartDate?.ToString()),
                NormalizeOptional(request.EndDate?.ToString()),
                NormalizeOptional(request.GroupBy),
                NormalizeOptional(request.GroupByType),
                request.GroupBySize.ToString(CultureInfo.InvariantCulture),
                NormalizeOptional(request.CategoryKey),
                NormalizeOptional(request.StatusKey),
                NormalizeOptional(request.LabelKey),
                NormalizeOptional(request.SearchText),
                request.TimeBucketSize.ToString(CultureInfo.InvariantCulture),
                NormalizeOptional(request.TimeBucket),
                request.ShowDrafts ? "1" : "0",
                request.ShowDeleted ? "1" : "0",
                request.OrderBy?.ToString().ToLowerInvariant() ?? String.Empty,
                request.OrderByDesc?.ToString().ToLowerInvariant() ?? String.Empty
            });

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
                var result = new StringBuilder(bytes.Length * 2);

                foreach (var value in bytes)
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));

                return result.ToString();
            }
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} is required.", parameterName);

            return value.Trim().ToLowerInvariant();
        }

        private static string NormalizeOptional(string value)
        {
            return String.IsNullOrWhiteSpace(value)
                ? String.Empty
                : value.Trim().ToLowerInvariant();
        }
    }
}
