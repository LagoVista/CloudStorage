using LagoVista.Core.Models.UIMetaData;

namespace LagoVista.CloudStorage.Models
{
    public class MongoDatabaseInfo
    {
        public string Name { get; set; }
    }

    public class MongoCollectionInfo
    {
        public string Name { get; set; }
        public long EstimatedDocumentCount { get; set; }
    }

    public class MongoDocumentInfo
    {
        public string Id { get; set; }
        public string Json { get; set; }
    }

    public class MongoQueryRequest : ListRequest
    {
        public string Filter { get; set; } = "{}";
        public string Sort { get; set; }
        public string Projection { get; set; }
    }

    public class MongoDocumentWriteRequest
    {
        public string Json { get; set; }
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
