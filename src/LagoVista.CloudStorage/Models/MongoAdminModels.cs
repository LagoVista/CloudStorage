using System.Collections.Generic;

namespace LagoVista.CloudStorage.Models
{
    public class MongoCollectionInfo
    {
        public string Name { get; set; }
        public long EstimatedDocumentCount { get; set; }
    }

    public class MongoQueryRequest
    {
        public string Filter { get; set; } = "{}";
        public string Sort { get; set; }
        public string Projection { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; } = 100;
    }

    public class MongoQueryResult
    {
        public long MatchedCount { get; set; }
        public int ReturnedCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public List<string> Documents { get; set; } = new List<string>();
    }

    public class MongoPatchRequest
    {
        public string Filter { get; set; } = "{}";
        public string Patch { get; set; }
        public long? ExpectedMatchCount { get; set; }
    }

    public class MongoPatchResult
    {
        public long MatchedCount { get; set; }
        public long ModifiedCount { get; set; }
    }
}
