using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cache
{
    public sealed class ValkeyAdminRepo : IValkeyAdminRepo
    {
        private readonly ICacheProviderSettings _settings;

        public ValkeyAdminRepo(ICacheProviderSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<IReadOnlyList<ValkeyKeyInfo>> ScanAsync(ValkeyScanRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new ValkeyScanRequest();
            var limit = Math.Max(1, Math.Min(request.PageSize, 500));

            using (var multiplexer = await ConnectAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var endpoint = multiplexer.GetEndPoints().FirstOrDefault()
                    ?? throw new InvalidOperationException("Valkey has no configured endpoint.");
                var server = multiplexer.GetServer(endpoint);
                var keys = server.Keys(request.Database, String.IsNullOrWhiteSpace(request.Pattern) ? "*" : request.Pattern, pageSize: limit)
                    .Take(limit)
                    .ToArray();

                var result = new List<ValkeyKeyInfo>(keys.Length);
                foreach (var key in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Add(await ReadKeyAsync(multiplexer.GetDatabase(request.Database), key).ConfigureAwait(false));
                }

                return result;
            }
        }

        public async Task<ValkeyKeyInfo> GetAsync(int database, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            using (var multiplexer = await ConnectAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await ReadKeyAsync(multiplexer.GetDatabase(database), key).ConfigureAwait(false);
            }
        }

        public async Task SetAsync(ValkeySetRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required.", nameof(request));

            using (var multiplexer = await ConnectAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var database = multiplexer.GetDatabase(request.Database);
                await database.StringSetAsync(request.Key, request.Value ?? String.Empty).ConfigureAwait(false);

                if (request.TtlSeconds.HasValue)
                {
                    await database.KeyExpireAsync(request.Key, TimeSpan.FromSeconds(request.TtlSeconds.Value)).ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> DeleteAsync(int database, string key, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

            using (var multiplexer = await ConnectAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await multiplexer.GetDatabase(database).KeyDeleteAsync(key).ConfigureAwait(false);
            }
        }

        private async Task<ConnectionMultiplexer> ConnectAsync()
        {
            if (!_settings.UseCache)
                throw new InvalidOperationException("Remote Valkey/Redis cache is disabled for this host.");

            var configuration = ConfigurationOptions.Parse(_settings.CacheSettings.Uri);
            configuration.AbortOnConnectFail = true;
            if (_settings.UseAuthentication)
                configuration.Password = _settings.Password;

            return await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false);
        }

        private static async Task<ValkeyKeyInfo> ReadKeyAsync(IDatabase database, RedisKey key)
        {
            var type = await database.KeyTypeAsync(key).ConfigureAwait(false);
            var ttl = await database.KeyTimeToLiveAsync(key).ConfigureAwait(false);
            string value;

            switch (type)
            {
                case RedisType.String:
                    value = (string)await database.StringGetAsync(key).ConfigureAwait(false);
                    break;
                case RedisType.Hash:
                    value = Newtonsoft.Json.JsonConvert.SerializeObject(
                        (await database.HashGetAllAsync(key).ConfigureAwait(false))
                            .ToDictionary(item => item.Name.ToString(), item => item.Value.ToString()));
                    break;
                case RedisType.List:
                    value = Newtonsoft.Json.JsonConvert.SerializeObject(
                        (await database.ListRangeAsync(key, 0, 499).ConfigureAwait(false)).Select(item => item.ToString()));
                    break;
                case RedisType.Set:
                    value = Newtonsoft.Json.JsonConvert.SerializeObject(
                        (await database.SetMembersAsync(key).ConfigureAwait(false)).Take(500).Select(item => item.ToString()));
                    break;
                case RedisType.SortedSet:
                    value = Newtonsoft.Json.JsonConvert.SerializeObject(
                        (await database.SortedSetRangeByRankWithScoresAsync(key, 0, 499).ConfigureAwait(false))
                            .Select(item => new { value = item.Element.ToString(), score = item.Score }));
                    break;
                default:
                    value = null;
                    break;
            }

            return new ValkeyKeyInfo
            {
                Key = key.ToString(),
                Type = type.ToString(),
                TtlSeconds = ttl.HasValue ? (long?)Math.Ceiling(ttl.Value.TotalSeconds) : null,
                Value = value
            };
        }
    }
}
