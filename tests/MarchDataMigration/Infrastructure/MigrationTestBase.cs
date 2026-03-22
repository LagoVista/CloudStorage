using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using MarchDataMigration.Generator.CodeGen;
using MarchDataMigration.Generator.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Infrastructure
{
    public abstract class MigrationTestBase
    {
        protected string SourceConnectionString { get; set; }
        protected string TargetConnectionString { get; set; }


        [SetUp]
        public async Task GenerateAsync()
        {
            var sourceSettings = TestConnections.ProdSQLServer;
            sourceSettings.UserName = Environment.GetEnvironmentVariable("SQL_READER_USER");
            sourceSettings.Password = Environment.GetEnvironmentVariable("SQL_READER_PASSWORD");
            SourceConnectionString = Build(sourceSettings);

            var targetSettings = TestConnections.DevSQLServer;
            targetSettings.ResourceName = "nuviot-dev-2026-03-22";
            TargetConnectionString = Build(targetSettings);
        }

        private static string Build(ConnectionSettings settings)
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
