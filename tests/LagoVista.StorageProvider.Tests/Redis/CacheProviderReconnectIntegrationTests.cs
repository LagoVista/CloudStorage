using LagoVista.CloudStorage;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Redis
{
    // Requires the existing Redis integration fixture on localhost:19079.
    // A local TCP relay initially remains offline, then begins forwarding to real Redis.
    // This tests recovery of the SAME multiplexer without touching or restarting Redis.
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Integration")]
    [TestCategory("RedisCacheProvider")]
    public sealed class CacheProviderReconnectIntegrationTests
    {
        [TestMethod]
        public async Task RedisBecomesAvailable_AfterConstruction_SameProviderReconnects()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var proxyPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var key = $"cache-recovery-{Guid.NewGuid():N}";
            using var provider = new CacheProvider(
                new EnabledSettings($"127.0.0.1:{proxyPort},connectTimeout=300,connectRetry=0,syncTimeout=400"),
                new Mock<IAdminLogger>().Object, new Mock<IAppConfig>().Object);

            Assert.IsNull(await provider.GetAsync(key));

            using var redis = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:19079,abortConnect=false");
            await redis.GetDatabase().StringSetAsync(key, "redis-is-back", TimeSpan.FromMinutes(1));

            using var relay = new RedisRelay(listener);
            relay.Start();

            var timeout = DateTime.UtcNow.AddSeconds(35);
            string recovered = null;
            while (DateTime.UtcNow < timeout)
            {
                recovered = await provider.GetAsync(key);
                if (recovered == "redis-is-back") break;
                await Task.Delay(250);
            }

            Assert.AreEqual("redis-is-back", recovered,
                "The existing disconnected multiplexer should recover and read from Redis after the relay opens.");

            await redis.GetDatabase().KeyDeleteAsync(key);
        }

        private sealed class EnabledSettings : ICacheProviderSettings
        {
            public EnabledSettings(string uri) => CacheSettings = new ConnectionSettings { Uri = uri };
            public bool UseCache => true;
            public bool UseAuthentication => false;
            public IConnectionSettings CacheSettings { get; }
            public string Password => String.Empty;
        }

        // A minimal TCP bridge to the Redis fixture. No Redis protocol emulation is needed.
        private sealed class RedisRelay : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _stop = new CancellationTokenSource();
            private Task _acceptLoop;

            public RedisRelay(TcpListener listener) => _listener = listener;

            public void Start()
            {
                _listener.Start();
                _acceptLoop = AcceptAsync();
            }

            private async Task AcceptAsync()
            {
                try
                {
                    while (!_stop.IsCancellationRequested)
                    {
                        var incoming = await _listener.AcceptTcpClientAsync(_stop.Token);
                        _ = RelayAsync(incoming);
                    }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
                catch (ObjectDisposedException) when (_stop.IsCancellationRequested) { }
            }

            private async Task RelayAsync(TcpClient incoming)
            {
                using (incoming)
                using (var outgoing = new TcpClient())
                {
                    try
                    {
                        await outgoing.ConnectAsync(IPAddress.Loopback, 19079, _stop.Token);
                        var upstream = incoming.GetStream();
                        var downstream = outgoing.GetStream();
                        var send = upstream.CopyToAsync(downstream, _stop.Token);
                        var receive = downstream.CopyToAsync(upstream, _stop.Token);
                        await Task.WhenAny(send, receive);
                    }
                    catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
                    catch (SocketException) when (_stop.IsCancellationRequested) { }
                    catch (System.IO.IOException) when (_stop.IsCancellationRequested) { }
                }
            }

            public void Dispose()
            {
                _stop.Cancel();
                _listener.Stop();
                _stop.Dispose();
            }
        }
    }
}
