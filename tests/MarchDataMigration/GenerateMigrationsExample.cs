using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using LegacyMigrationScaffolding.Generation;
using LegacyMigrationScaffolding.Schema;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace LegacyMigrationScaffolding.Usage
{
    [TestFixture]
    public class GenerateMigrationsExample
    {
        [Test]
        public Task GenerateAsync()
        {
            var settings = TestConnections.DevSQLServer;
            settings.ResourceName = "nuviot-dev-2026-03-22";
            var target = Build(settings);
            var source = Build(TestConnections.DevSQLServer);

            var scaffolder = new MigrationScaffolder();

            return scaffolder.GenerateAsync(new ScaffoldingOptions
            {
                SourceConnectionString = source,
                DestinationConnectionString = target,
                OutputFolder = @"D:\NuvIoT\cs.CloudStorage\tests\MarchDataMigration\Tables",
                Namespace = "LegacyMigration.Generated",
                SchemaName = "dbo"
            });
        }

        public static string Build(ConnectionSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var csb = new SqlConnectionStringBuilder
            {
                DataSource = settings.Uri,                 // e.g. "localhost,1433"
                InitialCatalog = settings.ResourceName,    // e.g. "Billing"
                UserID = settings.UserName,
                Password = settings.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                ConnectTimeout = settings.TimeoutInSeconds > 0 ? settings.TimeoutInSeconds : 30
            };

            return csb.ConnectionString;

        }
    }
}