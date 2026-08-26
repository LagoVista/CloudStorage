using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Relational.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Postgres
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("PostgresMetrics")]
    public class PostgresMetricsStoreIntegrationTests
    {
        [TestMethod]
        public async Task DefinitionRegistration_ProvisioningAndDI_SatisfyContract()
        {
            var settings = CreateSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IMetricsStore>();

            Assert.IsInstanceOfType<PostgresMetricsStore>(store);

            var definition = CreateDefinition();
            await store.RegisterDefinitionAsync(definition);

            var byId = await store.GetDefinitionAsync(definition.Id);
            var byKey = await store.GetDefinitionAsync(definition.Key);

            Assert.IsNotNull(byId);
            Assert.AreEqual(definition.Key, byId.Key);
            Assert.AreEqual(definition.Name, byId.Name);
            Assert.AreEqual(2, byId.Dimensions.Count);
            Assert.IsTrue(byId.Dimensions.Single(dimension => dimension.Key == "region").QueryImportant);
            Assert.AreEqual(definition.Id, byKey.Id);

            await using var connection = await OpenConnectionAsync(settings);
            await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM timescaledb_information.hypertables WHERE hypertable_schema = @schema AND hypertable_name = 'metric_records')", connection);
            command.Parameters.AddWithValue("schema", settings.SchemaName);
            Assert.IsTrue(Convert.ToBoolean(await command.ExecuteScalarAsync()));
        }

        [TestMethod]
        public async Task RecordAndBatch_AllAggregatesAndRangeIsolation_SatisfyContract()
        {
            var settings = CreateSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IMetricsStore>();
            var definition = CreateDefinition();
            await store.RegisterDefinitionAsync(definition);

            var start = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            await store.RecordAsync(CreateRecord("ORG1", definition.Id, start, 2, "east", "web"));
            await store.RecordBatchAsync(new[]
            {
                CreateRecord("ORG1", definition.Key, start.AddMinutes(1), 4, "east", "api"),
                CreateRecord("ORG1", definition.Key, start.AddMinutes(2), 8, "west", "web"),
                CreateRecord("ORG2", definition.Key, start.AddMinutes(1), 100, "east", "web"),
                CreateRecord("ORG1", definition.Key, start.AddHours(2), 50, "east", "web")
            });

            var end = start.AddMinutes(10);
            Assert.AreEqual(14d, await QuerySingleValueAsync(store, "ORG1", definition.Id, start, end, MetricAggregate.Sum), 0.0001);
            Assert.AreEqual(3d, await QuerySingleValueAsync(store, "ORG1", definition.Key, start, end, MetricAggregate.Count), 0.0001);
            Assert.AreEqual(14d / 3d, await QuerySingleValueAsync(store, "ORG1", definition.Key, start, end, MetricAggregate.Average), 0.0001);
            Assert.AreEqual(2d, await QuerySingleValueAsync(store, "ORG1", definition.Key, start, end, MetricAggregate.Minimum), 0.0001);
            Assert.AreEqual(8d, await QuerySingleValueAsync(store, "ORG1", definition.Key, start, end, MetricAggregate.Maximum), 0.0001);

            var narrow = await store.QueryAsync(new MetricQuery("ORG1", definition.Key, start.AddMinutes(1), start.AddMinutes(2), MetricAggregate.Sum));
            Assert.AreEqual(12d, narrow.Values.Single().Value, 0.0001);
        }

        [TestMethod]
        public async Task BucketedDimensionFilterAndGrouping_SatisfyContract()
        {
            var settings = CreateSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IMetricsStore>();
            var definition = CreateDefinition();
            await store.RegisterDefinitionAsync(definition);

            var start = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            await store.RecordBatchAsync(new[]
            {
                CreateRecord("ORG1", definition.Key, start.AddMinutes(5), 2, "east", "web"),
                CreateRecord("ORG1", definition.Key, start.AddMinutes(25), 3, "east", "api"),
                CreateRecord("ORG1", definition.Key, start.AddHours(1).AddMinutes(5), 5, "west", "web"),
                CreateRecord("ORG1", definition.Key, start.AddHours(1).AddMinutes(10), 7, "east", "web")
            });

            var query = new MetricQuery("ORG1", definition.Key, start, start.AddHours(2), MetricAggregate.Sum, TimeSpan.FromHours(1), new[] { new MetricDimensionFilter("channel", "web") }, new[] { "region" });
            var result = await store.QueryAsync(query);

            Assert.AreEqual(3, result.Values.Count);
            AssertMetricValue(result, start, "east", 2);
            AssertMetricValue(result, start.AddHours(1), "east", 7);
            AssertMetricValue(result, start.AddHours(1), "west", 5);
        }

        [TestMethod]
        public async Task DefinitionAndDimensionValidation_RejectInvalidWritesAndQueries()
        {
            var settings = CreateSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IMetricsStore>();
            var definition = CreateDefinition();
            var start = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.RecordAsync(CreateRecord("ORG1", "missing-metric", start, 1, "east", "web")));

            await store.RegisterDefinitionAsync(definition);
            var invalidDimensions = new Dictionary<string, string> { ["rogue"] = "value" };
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.RecordAsync(new MetricRecord(Guid.NewGuid().ToString("N"), "ORG1", "Organization 1", definition.Key, start, 1, invalidDimensions)));

            var valid = CreateRecord("ORG1", definition.Key, start, 2, "east", "web");
            var invalid = new MetricRecord(Guid.NewGuid().ToString("N"), "ORG1", "Organization 1", definition.Key, start.AddMinutes(1), 3, invalidDimensions);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.RecordBatchAsync(new[] { valid, invalid }));

            var countAfterRejectedBatch = await QuerySingleValueAsync(store, "ORG1", definition.Key, start.AddMinutes(-1), start.AddMinutes(2), MetricAggregate.Count);
            Assert.AreEqual(0d, countAfterRejectedBatch, 0.0001);

            var invalidQuery = new MetricQuery("ORG1", definition.Key, start, start.AddMinutes(2), dimensions: new[] { new MetricDimensionFilter("rogue", "value") });
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.QueryAsync(invalidQuery));
        }

        private static ServiceProvider CreateServices(TestMetricsStorageSettings settings)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMetricsStorageSettings>(settings);
            services.AddPostgresMetricsStore();
            return services.BuildServiceProvider();
        }

        private static MetricDefinition CreateDefinition()
        {
            return new MetricDefinition(Guid.NewGuid().ToString("N"), $"messages-{Guid.NewGuid():N}", "Messages", new[]
            {
                new MetricDimensionDefinition("region", "Region", queryImportant: true),
                new MetricDimensionDefinition("channel", "Channel")
            });
        }

        private static MetricRecord CreateRecord(string organizationId, string metric, DateTime timestamp, double value, string region, string channel)
        {
            return new MetricRecord(Guid.NewGuid().ToString("N"), organizationId, $"Organization {organizationId}", metric, timestamp, value, new Dictionary<string, string>
            {
                ["region"] = region,
                ["channel"] = channel
            });
        }

        private static async Task<double> QuerySingleValueAsync(IMetricsStore store, string organizationId, string metric, DateTime start, DateTime end, MetricAggregate aggregate)
        {
            var result = await store.QueryAsync(new MetricQuery(organizationId, metric, start, end, aggregate));
            return result.Values.Single().Value;
        }

        private static void AssertMetricValue(MetricQueryResult result, DateTime timestamp, string region, double expectedValue)
        {
            var value = result.Values.Single(item => item.Timestamp == timestamp && item.Dimensions.TryGetValue("region", out var actualRegion) && actualRegion == region);
            Assert.AreEqual(expectedValue, value.Value, 0.0001);
        }

        private static TestMetricsStorageSettings CreateSettings()
        {
            return new TestMetricsStorageSettings($"metrics_{Guid.NewGuid():N}");
        }

        private static async Task<NpgsqlConnection> OpenConnectionAsync(TestMetricsStorageSettings settings)
        {
            var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder
            {
                Host = settings.HostName,
                Port = settings.Port,
                Username = settings.UserName,
                Password = settings.Password,
                Database = "postgres"
            }.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private sealed class TestMetricsStorageSettings : IMetricsStorageSettings
        {
            public TestMetricsStorageSettings(string schemaName)
            {
                SchemaName = schemaName;
            }

            public string HostName => "127.0.0.1";
            public string UserName => "postgres";
            public string Password => String.Empty;
            public int Port => 19043;
            public string SchemaName { get; }
        }
    }
}
