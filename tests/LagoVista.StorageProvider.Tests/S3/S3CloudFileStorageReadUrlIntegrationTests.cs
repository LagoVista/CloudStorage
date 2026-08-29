using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.File;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.S3
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("S3CloudFileStorage")]
    public sealed class S3CloudFileStorageReadUrlIntegrationTests
    {
        private const string ContainerName = "cloudstorage-tests";
        private IS3ObjectStorageConnectionSettings _settings;
        private IAdminLogger _logger;

        [TestInitialize]
        public void Setup()
        {
            _settings = new TestS3Settings();
            _logger = Mock.Of<IAdminLogger>();
        }

        [TestMethod]
        public async Task CreateReadUrlAsync_ProducesWorkingUnauthenticatedSignedUrl()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var fileName = $"/signed urls/{Guid.NewGuid():N}.txt";
            var expected = "short-lived-public-read";

            var add = await client.AddFileAsync(ContainerName, fileName, expected, "text/plain");
            Assert.IsTrue(add.Successful);

            var readUrl = await client.CreateReadUrlAsync(ContainerName, fileName, TimeSpan.FromMinutes(2));
            Assert.IsTrue(readUrl.Successful);
            Assert.IsNotNull(readUrl.Result);
            Assert.AreEqual("localhost", readUrl.Result.Host);
            Assert.AreEqual(19090, readUrl.Result.Port);
            StringAssert.Contains(readUrl.Result.Query, "X-Amz-Expires=120");

            using var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync(readUrl.Result);
            Assert.AreEqual(expected, content);
        }

        [TestMethod]
        public async Task CreateReadUrlAsync_RejectsInvalidLifetimes()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);

            var zero = await client.CreateReadUrlAsync(ContainerName, "anything.txt", TimeSpan.Zero);
            Assert.IsFalse(zero.Successful);

            var negative = await client.CreateReadUrlAsync(ContainerName, "anything.txt", TimeSpan.FromSeconds(-1));
            Assert.IsFalse(negative.Successful);

            var tooLong = await client.CreateReadUrlAsync(ContainerName, "anything.txt", TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));
            Assert.IsFalse(tooLong.Successful);
        }

        [TestMethod]
        public async Task CreateReadUrlAsync_RequiresExistingObject()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var result = await client.CreateReadUrlAsync(ContainerName, $"missing/{Guid.NewGuid():N}.txt", TimeSpan.FromMinutes(1));
            Assert.IsFalse(result.Successful);
        }

        private sealed class TestS3Settings : IS3ObjectStorageConnectionSettings
        {
            public string Host => "localhost";
            public int Port => 19090;
            public string AccessKey => "nuviot-test";
            public string SecretKey => "nuviot-test-password";
            public bool UseTls => false;
            public string Region => null;
            public string PublicHost => "localhost";
            public int PublicPort => 19090;
            public bool PublicUseTls => false;
        }
    }
}
