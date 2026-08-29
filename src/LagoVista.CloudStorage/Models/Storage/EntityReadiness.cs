using LagoVista.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models.Storage
{
    public sealed class ReadinessScorecardProjection
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string EntityType { get; set; }

        public EntityHeader OwnerOrganization { get; set; }

        public JToken ReadinessChecks { get; set; }
    }

    public sealed class ReadinessCandidateProjection
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string EntityType { get; set; }

        public EntityHeader OwnerOrganization { get; set; }

        public JToken ChecklistStatus { get; set; }

        public JToken ReadinessChecks { get; set; }

        public JToken MasterStatus { get; set; }

        [JsonProperty("_etag")]
        public string CosmosETag { get; set; }

        public string ETag { get; set; }

        [JsonIgnore]
        public string StorageETag => !String.IsNullOrWhiteSpace(CosmosETag) ? CosmosETag : ETag;
    }
}
