using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LagoVista.StorageProvider.Tests.DependencyInjection
{
    [TestClass]
    public class CloudStorageStartupTests
    {
        [TestMethod]
        public void ConfigureServices_DoesNotRegisterCloudStorageServicesMoreThanOnce()
        {
            var services = new ServiceCollection();

            LagoVista.CloudStorage.Startup.ConfigureServices(services);

            var cloudStorageAssembly = typeof(LagoVista.CloudStorage.Startup).Assembly;

            var duplicates = services
                .Where(descriptor =>
                    descriptor.ServiceType.Assembly == cloudStorageAssembly ||
                    descriptor.ImplementationType?.Assembly == cloudStorageAssembly ||
                    descriptor.ImplementationInstance?.GetType().Assembly == cloudStorageAssembly)
                .Where(descriptor => descriptor.ServiceType != typeof(IPlatformSmokeTest))
                .GroupBy(descriptor => descriptor.ServiceType)
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key.FullName} ({group.Count()} registrations)")
                .OrderBy(value => value)
                .ToArray();

            Assert.AreEqual(
                0,
                duplicates.Length,
                $"Duplicate CloudStorage DI registrations found:{Environment.NewLine}{String.Join(Environment.NewLine, duplicates)}");
        }

        [TestMethod]
        public void ConfigureServices_RegistersAllPlatformSmokeTestsOnce()
        {
            var services = new ServiceCollection();

            LagoVista.CloudStorage.Startup.ConfigureServices(services);

            var smokeTests = services
                .Where(descriptor => descriptor.ServiceType == typeof(IPlatformSmokeTest))
                .Select(descriptor => descriptor.ImplementationType)
                .ToArray();

            Assert.AreEqual(4, smokeTests.Length);
            Assert.AreEqual(4, smokeTests.Distinct().Count());
        }
    }
}
