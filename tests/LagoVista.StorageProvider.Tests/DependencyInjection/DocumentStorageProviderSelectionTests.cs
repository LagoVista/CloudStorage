using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace LagoVista.StorageProvider.Tests.DependencyInjection;

[TestClass]
public sealed class DocumentStorageProviderSelectionTests
{
    [TestMethod]
    public void MongoSelectionDoesNotResolveCosmosClient()
    {
        var mongo = new Mock<IMongoDocumentStorageClient>();
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStorageProviderSettings>(new ProviderSettings(DocumentStorageClientType.Mongo));
        services.AddSingleton(mongo.Object);
        services.AddScoped<IDocumentStorageClientProvider, DocumentStorageClientProvider>();

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IDocumentStorageClientProvider>().GetClient();

        Assert.AreSame(mongo.Object, storage);
    }

    [TestMethod]
    public void CosmosSelectionDoesNotResolveMongoClient()
    {
        var cosmos = new Mock<ICosmosDocumentStorageClient>();
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStorageProviderSettings>(new ProviderSettings(DocumentStorageClientType.Cosmos));
        services.AddSingleton(cosmos.Object);
        services.AddScoped<IDocumentStorageClientProvider, DocumentStorageClientProvider>();

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IDocumentStorageClientProvider>().GetClient();

        Assert.AreSame(cosmos.Object, storage);
    }

    [TestMethod]
    public void DefaultConnectionSettingsDoNotRequireCosmosWhenNotRequested()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DefaultTableStorage:Name"] = "legacy-table-account",
                ["DefaultTableStorage:AccessKey"] = "legacy-table-key",
                ["CheckPointStorage:Name"] = "legacy-checkpoint-account",
                ["CheckPointStorage:AccessKey"] = "legacy-checkpoint-key"
            })
            .Build();

        var settings = new DefaultConnectionSettings(configuration);

        Assert.AreEqual("legacy-table-account", settings.DefaultTableStorageSettings.AccountId);
        Assert.AreEqual("legacy-table-key", settings.DefaultTableStorageSettings.AccessKey);
    }

    private sealed class ProviderSettings : IDocumentStorageProviderSettings
    {
        public ProviderSettings(DocumentStorageClientType provider) => Provider = provider;
        public DocumentStorageClientType Provider { get; }
    }
}
