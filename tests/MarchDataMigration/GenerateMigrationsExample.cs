using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using MarchDataMigration.Generator.CodeGen;
using MarchDataMigration.Generator.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyMigrationScaffolding.Usage
{
    [TestFixture]
    public class GenerateMigrationsExample
    {
        [Test]
        public async Task GenerateAsync()
        {
            var sourceSettings = TestConnections.ProdSQLServer;
            sourceSettings.UserName = Environment.GetEnvironmentVariable("SQL_READER_USER");
            sourceSettings.Password = Environment.GetEnvironmentVariable("SQL_READER_PASSWORD");
            var source = Build(sourceSettings);

            var targetSettings = TestConnections.DevSQLServer;
            targetSettings.ResourceName = "nuviot-dev-2026-03-22";
            var target = Build(targetSettings);

            var request = new MigrationGeneratorRequest()
            {
                OutputRootPath = @"D:\NuvIoT\cs.CloudStorage\Tests\MarchDataMigration\Generated",
                SourceConnectionString = source,
                TargetConnectionString = target,
                IncludeTables = new[] {"PayrollSummary/PayRun"}
            };

            var generator = new MigrationArtifactGenerator();
            await generator.GenerateAsync(request, CancellationToken.None);
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