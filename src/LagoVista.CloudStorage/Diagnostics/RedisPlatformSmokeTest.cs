using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    public sealed class RedisPlatformSmokeTest : IPlatformSmokeTest
    {
        private readonly ICacheProviderSettings _settings;

        public RedisPlatformSmokeTest(ICacheProviderSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Key => "cache.redis";
        public string Name => "Redis Cache";
        public string Category => "Data Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_settings.UseCache)
            {
                return new PlatformSmokeTestResult
                {
                    Status = PlatformSmokeTestStatus.Skipped,
                    Message = "Remote cache is disabled for this host."
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            var configuration = ConfigurationOptions.Parse(_settings.CacheSettings.Uri);
            configuration.AbortOnConnectFail = true;
            configuration.ConnectTimeout = Math.Min(configuration.ConnectTimeout, 5000);
            configuration.SyncTimeout = Math.Min(configuration.SyncTimeout, 5000);

            if (_settings.UseAuthentication)
            {
                if (String.IsNullOrWhiteSpace(_settings.Password))
                    throw new InvalidOperationException("Redis authentication is enabled but no password is configured.");

                configuration.Password = _settings.Password;
            }

            using (var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var latency = await multiplexer.GetDatabase().PingAsync().ConfigureAwait(false);

                return new PlatformSmokeTestResult
                {
                    Status = PlatformSmokeTestStatus.Passed,
                    Target = String.Join(", ", configuration.EndPoints.Select(endpoint => endpoint.ToString())),
                    Message = $"Redis PING succeeded in {Math.Round(latency.TotalMilliseconds)} ms."
                };
            }
        }
    }
}
