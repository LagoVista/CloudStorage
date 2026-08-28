using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.S3
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("S3CloudFileStorage")]
    public sealed class S3CloudFileStorageRetryIntegrationTests
    {
        private const string ContainerName = "cloudstorage-tests";
        private const int TargetPort = 19090;

        [TestMethod]
        public async Task AddFileAsync_RecoversAfterTransientConnectionFailure()
        {
            var proxyPort = GetAvailablePort();
            var logger = new Mock<IAdminLogger>();
            var client = new S3CloudFileStorageClient(new TestS3Settings(proxyPort), logger.Object);
            var fileName = $"retry/write/{Guid.NewGuid():N}.txt";
            var expected = Encoding.UTF8.GetBytes("retry-write");

            var operation = client.AddFileAsync(ContainerName, fileName, expected, "text/plain");
            await Task.Delay(400);

            await using var proxy = new TcpForwardingProxy(proxyPort, "127.0.0.1", TargetPort);
            proxy.Start();

            var result = await operation;
            Assert.IsTrue(result.Successful);
            Assert.IsTrue(logger.Invocations.Any(invocation => invocation.Method.Name == nameof(IAdminLogger.AddCustomEvent)), "The client succeeded, but its retry warning path was never entered.");

            var verificationClient = new S3CloudFileStorageClient(new TestS3Settings(TargetPort), Mock.Of<IAdminLogger>());
            var read = await verificationClient.GetFileAsync(ContainerName, fileName);
            Assert.IsTrue(read.Successful);
            CollectionAssert.AreEqual(expected, read.Result);
        }

        [TestMethod]
        public async Task GetFileAsync_RecoversAfterTransientConnectionFailure()
        {
            var logger = new Mock<IAdminLogger>();
            var seedClient = new S3CloudFileStorageClient(new TestS3Settings(TargetPort), Mock.Of<IAdminLogger>());
            var fileName = $"retry/read/{Guid.NewGuid():N}.txt";
            var expected = Encoding.UTF8.GetBytes("retry-read");
            var add = await seedClient.AddFileAsync(ContainerName, fileName, expected, "text/plain");
            Assert.IsTrue(add.Successful);

            var proxyPort = GetAvailablePort();
            var client = new S3CloudFileStorageClient(new TestS3Settings(proxyPort), logger.Object);
            var operation = client.GetFileAsync(ContainerName, fileName);
            await Task.Delay(400);

            await using var proxy = new TcpForwardingProxy(proxyPort, "127.0.0.1", TargetPort);
            proxy.Start();

            var result = await operation;
            Assert.IsTrue(result.Successful);
            CollectionAssert.AreEqual(expected, result.Result);
            Assert.IsTrue(logger.Invocations.Any(invocation => invocation.Method.Name == nameof(IAdminLogger.AddCustomEvent)), "The client succeeded, but its retry warning path was never entered.");
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed class TestS3Settings : IS3ObjectStorageConnectionSettings
        {
            public TestS3Settings(int port)
            {
                Port = port;
            }

            public string Host => "localhost";
            public int Port { get; }
            public string AccessKey => "nuviot-test";
            public string SecretKey => "nuviot-test-password";
            public bool UseTls => false;
            public string Region => null;
            public string PublicHost => "localhost";
            public int PublicPort => Port;
            public bool PublicUseTls => false;
        }

        private sealed class TcpForwardingProxy : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly string _targetHost;
            private readonly int _targetPort;
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            private Task _acceptLoop;

            public TcpForwardingProxy(int listenPort, string targetHost, int targetPort)
            {
                _listener = new TcpListener(IPAddress.Loopback, listenPort);
                _targetHost = targetHost;
                _targetPort = targetPort;
            }

            public void Start()
            {
                _listener.Start();
                _acceptLoop = AcceptLoopAsync(_cancellationTokenSource.Token);
            }

            public async ValueTask DisposeAsync()
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();

                if (_acceptLoop != null)
                {
                    try
                    {
                        await _acceptLoop;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (SocketException)
                    {
                    }
                }

                _cancellationTokenSource.Dispose();
            }

            private async Task AcceptLoopAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient downstream;
                    try
                    {
                        downstream = await _listener.AcceptTcpClientAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }

                    _ = ForwardAsync(downstream, cancellationToken);
                }
            }

            private async Task ForwardAsync(TcpClient downstream, CancellationToken cancellationToken)
            {
                using (downstream)
                using (var upstream = new TcpClient())
                {
                    await upstream.ConnectAsync(_targetHost, _targetPort, cancellationToken);
                    using var downstreamStream = downstream.GetStream();
                    using var upstreamStream = upstream.GetStream();

                    var downstreamToUpstream = downstreamStream.CopyToAsync(upstreamStream, cancellationToken);
                    var upstreamToDownstream = upstreamStream.CopyToAsync(downstreamStream, cancellationToken);

                    await Task.WhenAny(downstreamToUpstream, upstreamToDownstream);
                }
            }
        }
    }
}
