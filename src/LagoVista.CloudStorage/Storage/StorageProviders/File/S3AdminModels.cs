using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage.StorageProviders.File
{
    public sealed class S3AdminObjectInfo
    {
        public string Key { get; set; }
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public string LastModified { get; set; }
        public string ETag { get; set; }
        public string ContentType { get; set; }
    }

    public sealed class S3AdminObjectList
    {
        public string Bucket { get; set; }
        public string Prefix { get; set; }
        public List<S3AdminObjectInfo> Items { get; set; } = new List<S3AdminObjectInfo>();
        public bool Truncated { get; set; }
    }
}
