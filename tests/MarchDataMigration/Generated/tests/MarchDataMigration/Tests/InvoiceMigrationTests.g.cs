using MarchDataMigration.Generated.Invoice;
using MarchDataMigration.Infrastructure;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Tests.Working
{
    [TestFixture]
    public class InvoiceMigrationTests : MigrationTestBase
    {
        [Test]
        public async Task Migrate_Invoice()
        {
            var service = new InvoiceMigrationService();
            var result = await service.MigrateAsync(SourceConnectionString, TargetConnectionString, CancellationToken.None);

            TestContext.WriteLine($"table={result.TableName} success={result.Success} source={result.SourceCount} inserted={result.InsertedCount} target={result.TargetCountAfterInsert} duration={result.Duration}");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceCount, Is.EqualTo(result.InsertedCount), "Source/insert count mismatch.");
            Assert.That(result.TargetCountAfterInsert, Is.EqualTo(result.InsertedCount), "Target/insert count mismatch.");
        }
    }
}
