using System.Collections.Generic;

namespace LagoVista.CloudStorage.Models
{
    public class CassandraTableInfo
    {
        public string Name { get; set; }
    }

    public class CassandraQueryRequest
    {
        public string Cql { get; set; }
        public int PageSize { get; set; } = 100;
    }

    public class CassandraQueryResult
    {
        public List<string> Columns { get; set; } = new List<string>();
        public List<string> Rows { get; set; } = new List<string>();
    }

    public class CassandraExecuteRequest
    {
        public string Cql { get; set; }
    }

    public class ValkeyKeyInfo
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public long? TtlSeconds { get; set; }
        public string Value { get; set; }
    }

    public class ValkeyScanRequest
    {
        public int Database { get; set; }
        public string Pattern { get; set; } = "*";
        public int PageSize { get; set; } = 100;
    }

    public class ValkeySetRequest
    {
        public int Database { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public long? TtlSeconds { get; set; }
    }
}
