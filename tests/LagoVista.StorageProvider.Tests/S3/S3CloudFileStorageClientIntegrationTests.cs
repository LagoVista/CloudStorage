using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using LagoVista.CloudStorage.RuntimeControls;
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
    [TestCategory("Integration")]
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
        public async Task RoutineS3Logs_FollowRuntimeControlWithoutGlobalVerbose()
        {
            var enabled = false;
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(control => control.IsEnabled(RuntimeControlCatalog.S3FileStorageDiagnostics))
                .Returns(() => enabled);
            var logger = new Mock<IAdminLogger>();
            var client = new S3CloudFileStorageClient(_settings, logger.Object, controls.Object);
            var fileName = $"diagnostics/{Guid.NewGuid():N}.txt";
            var expected = Encoding.UTF8.GetBytes("S3 diagnostics");

            var added = await client.AddFileAsync(ContainerName, fileName, expected);
            Assert.IsTrue(added.Successful);
            Assert.AreEqual(0, RoutineLogCount(logger));

            enabled = true;
            var read = await client.GetFileAsync(ContainerName, fileName);
            Assert.IsTrue(read.Successful);
            CollectionAssert.AreEqual(expected, read.Result);
            Assert.AreEqual(2, RoutineLogCount(logger));

            enabled = false;
            var deleted = await client.DeleteFileAsync(ContainerName, fileName);
            Assert.IsTrue(deleted.Successful);
            Assert.AreEqual(2, RoutineLogCount(logger));
        }

        private static int RoutineLogCount(Mock<IAdminLogger> logger)
        {
            return logger.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(IAdminLogger.AddCustomEvent) &&
                Equals(invocation.Arguments[0], LagoVista.Core.PlatformSupport.LogLevel.Verbose) &&
                Equals(invocation.Arguments[1], RuntimeControlCatalog.S3FileStorageDiagnostics));
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
        public async Task RejectUpdates_CreatesOnceThenRejectsReplacementAndPreservesExistingObject()
        {
            var client = new S3CloudFileStorageClient(_settings, _logger);
            var fileName = $"reject/{Guid.NewGuid():N}.txt";
            var original = Encoding.UTF8.GetBytes("original");
            var replacement = Encoding.UTF8.GetBytes("replacement");

            var add = await client.AddFileAsync(ContainerName, fileName, original, rejectUpdates: true);
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
