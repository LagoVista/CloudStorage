using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using LagoVista.Relational.DataContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarchDataMigration
{
    public class SchemaCreation
    {
        public BillingDataContext CreateContext()
        {

            var settings = TestConnections.DevSQLServer;
            settings.ResourceName = "nuviot-dev-2026-03-22";
            var cs = Build(settings);

            var opts = new DbContextOptionsBuilder<BillingDataContext>()
                   .UseSqlServer(cs)
                   .EnableSensitiveDataLogging()
                   .Options;

            return new BillingDataContext(opts);
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

        [Test]
        public async Task CreateDatabase()
        {
            var ctx = CreateContext();
            var sql =  ctx.Database.GenerateCreateScript();
            await System.IO.File.WriteAllTextAsync(@"X:\BillingSchema.sql", sql);
        }
    }
}
