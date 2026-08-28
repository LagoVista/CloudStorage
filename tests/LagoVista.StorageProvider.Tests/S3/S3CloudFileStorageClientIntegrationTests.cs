using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.File;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.S3
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("S3CloudFileStorage")]
    public sealed class S3CloudFileStorageClientIntegrationTests
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
        public async Task BinaryRoundTrip_AutoCreatesBucketAndDeletesObject()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var fileName = $"binary/{Guid.NewGuid():N}.bin";
            var expected = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

            var add = await client.AddFileAsync(ContainerName, fileName, expected, "application/octet-stream");
            Assert.IsTrue(add.Successful);

            var get = await client.GetFileAsync(ContainerName, fileName);
            Assert.IsTrue(get.Successful);
            CollectionAssert.AreEqual(expected, get.Result);

            var delete = await client.DeleteFileAsync(ContainerName, fileName);
            Assert.IsTrue(delete.Successful);
        }

        [TestMethod]
        public async Task StringAddAndUpdate_RoundTripLatestContent()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var fileName = $"text/{Guid.NewGuid():N}.txt";

            var add = await client.AddFileAsync(ContainerName, fileName, "first", "text/plain", "no-cache");
            Assert.IsTrue(add.Successful);

            var update = await client.UpdateFileAsync(ContainerName, fileName, "second", "text/plain", "no-cache");
            Assert.IsTrue(update.Successful);

            var get = await client.GetFileAsync(ContainerName, fileName);
            Assert.IsTrue(get.Successful);
            Assert.AreEqual("second", Encoding.UTF8.GetString(get.Result));
        }

        [TestMethod]
        public async Task BoundContainerOverloads_NormalizeLeadingSlashAndRoundTrip()
        {
            var client = new S3CloudFileStorageClient(_settings,  _logger);
            var fileName = $"/folder with spaces/{Guid.NewGuid():N}.txt";
            var expected = Encoding.UTF8.GetBytes("bound-container");

            var add = await client.AddFileAsync(ContainerName,fileName, expected, "text/plain");
            Assert.IsTrue(add.Successful);
            Assert.IsNotNull(add.Result);
            StringAssert.Contains(add.Result.AbsoluteUri, "folder%20with%20spaces");

            var get = await client.GetFileAsync(ContainerName,fileName);
            Assert.IsTrue(get.Successful);
            CollectionAssert.AreEqual(expected, get.Result);

            var delete = await client.DeleteFileAsync(ContainerName, fileName);
            Assert.IsTrue(delete.Successful);
        }

        [TestMethod]
        public async Task RejectUpdates_FailsSafelyAndPreservesExistingObject()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var fileName = $"reject/{Guid.NewGuid():N}.txt";
            var original = Encoding.UTF8.GetBytes("original");
            var replacement = Encoding.UTF8.GetBytes("replacement");

            var add = await client.AddFileAsync(ContainerName, fileName, original);
            Assert.IsTrue(add.Successful);

            var rejected = await client.AddFileAsync(ContainerName, fileName, replacement, rejectUpdates: true);
            Assert.IsFalse(rejected.Successful);

            var get = await client.GetFileAsync(ContainerName, fileName);
            Assert.IsTrue(get.Successful);
            CollectionAssert.AreEqual(original, get.Result);
        }


        private sealed class TestS3Settings : IS3ObjectStorageConnectionSettings
        {
            public string Host => "localhost";
            public int Port => 19090;
            public string AccessKey => "nuviot-test";
            public string SecretKey => "nuviot-test-password";
            public bool UseTls => false;
            public string Region => null;
            public string PublicHost => "public.localhost";
            public int PublicPort => 443;
            public bool PublicUseTls => true;
        }
    }
}
