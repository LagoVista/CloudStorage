using MarchDataMigration.Generator.CodeGen;
using MarchDataMigration.Generator.Models;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.ScemaGeneration
{
    [TestFixture]
    public class GenerateMigrationArtifactsTests
    {
        [Test]
        public async Task Generate_Migration_Files()
        {
            var request = new MigrationGeneratorRequest
            {
                SourceConnectionString = "<SOURCE CONNECTION STRING>",
                TargetConnectionString = "<TARGET CONNECTION STRING>",
                OutputRootPath = @"<OUTPUT ROOT PATH>",
                TestsProjectPath = "tests/MarchDataMigration"
                // IncludeTables = new[] { "AppUser", "Org", "Customers" }
            };

            var generator = new MigrationArtifactGenerator();
            await generator.GenerateAsync(request, CancellationToken.None);
        }
    }
}
