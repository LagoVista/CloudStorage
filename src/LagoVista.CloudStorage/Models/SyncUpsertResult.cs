using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class SyncUpsertResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        // Concurrency token, if the backing store supports it.
        [JsonProperty("etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        // 200/201-style semantics for stores that expose it (Cosmos does).
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        // Optional diagnostics.
        [JsonProperty("requestCharge", NullValueHandling = NullValueHandling.Ignore)]
        public double? RequestCharge { get; set; }
    }

}
