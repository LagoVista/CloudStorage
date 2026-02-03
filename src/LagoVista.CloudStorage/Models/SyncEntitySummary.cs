using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class SyncEntitySummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("entityType")]
        public string EntityType { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("revision")]
        public int Revision { get; set; }

        [JsonProperty("revisionTimeStamp")]
        public string RevisionTimeStamp { get; set; }

        // Cosmos uses "_etag" on the wire; other stores can map this as needed.
        [JsonProperty(PropertyName = "_etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        [JsonProperty("isDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("isDraft")]
        public bool IsDraft { get; set; }

        [JsonProperty("lastUpdatedDate")]
        public string LastUpdatedDate { get; set; }
    }


    public class SyncJsonEnvelope
    {
        [JsonProperty("json")]
        public string Json { get; set; }
    }

    public class SyncUpsertRequest
    {
        [JsonProperty("json")]
        public string Json { get; set; }

        [JsonProperty("expectedETag", NullValueHandling = NullValueHandling.Ignore)]
        public string ExpectedETag { get; set; }
    }
}
