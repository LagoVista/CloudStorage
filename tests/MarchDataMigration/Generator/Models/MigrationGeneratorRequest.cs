using System.Collections.Generic;

namespace MarchDataMigration.Generator.Models
{
    public sealed class MigrationGeneratorRequest
    {
        public string SourceConnectionString { get; init; }
        public string TargetConnectionString { get; init; }
        public string OutputRootPath { get; init; }
        public IReadOnlyCollection<string> IncludeTables { get; init; }
        public string TestsProjectPath { get; init; } = "tests/MarchDataMigration";
    }
}
