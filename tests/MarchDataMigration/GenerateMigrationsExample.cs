using LegacyMigrationScaffolding.Generation;
using LegacyMigrationScaffolding.Schema;
using System.Threading.Tasks;

namespace LegacyMigrationScaffolding.Usage
{
    [TestFixture]
    public class GenerateMigrationsExample
    {
        [Test]
        public Task GenerateAsync()
        {
            var scaffolder = new MigrationScaffolder();

            return scaffolder.GenerateAsync(new ScaffoldingOptions
            {
               // SourceConnectionString = TestConnections.DevSQLServer.,
               // DestinationConnectionString = destinationConnectionString,
               // OutputFolder = outputFolder,
                Namespace = "LegacyMigration.Generated",
                SchemaName = "dbo"
            });
        }
    }
}