using System;
using System.Collections.Generic;
using System.Text;

namespace MarchDataMigration
{
    public sealed class TableMigrationResult
    {
        public string TableName { get; init; }
        public int SourceCount { get; init; }
        public int InsertedCount { get; init; }
        public int TargetCountAfterInsert { get; init; }
        public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public string ErrorMessage { get; init; }

        public static TableMigrationResult Failure(string tableName, TimeSpan duration, Exception ex)
        {
            return new TableMigrationResult
            {
                TableName = tableName,
                Duration = duration,
                Success = false,
                ErrorMessage = ex.ToString()
            };
        }
    }
}
