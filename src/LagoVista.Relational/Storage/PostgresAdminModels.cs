using System.Collections.Generic;

namespace LagoVista.Relational.Storage
{
    public sealed class PostgresDatabaseInfo
    {
        public string Name { get; set; }
    }

    public sealed class PostgresTableInfo
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public sealed class PostgresQueryRequest
    {
        public string Database { get; set; }
        public string Sql { get; set; }
        public int PageSize { get; set; } = 100;
    }

    public sealed class PostgresQueryResult
    {
        public List<string> Columns { get; set; } = new List<string>();
        public List<string> Rows { get; set; } = new List<string>();
        public bool Truncated { get; set; }
    }
}
