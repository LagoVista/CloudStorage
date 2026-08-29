using LagoVista.Core.Models;
using Newtonsoft.Json;

namespace LagoVista.CloudStorage.Models
{
    internal class SyncEntitySummaryProjection
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("EntityType")]
        public string EntityType { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Revision")]
        public int Revision { get; set; }

        [JsonProperty("RevisionTimeStamp")]
        public string RevisionTimeStamp { get; set; }

        [JsonProperty(PropertyName = "_etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("IsDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("IsDraft")]
        public bool IsDraft { get; set; }

        [JsonProperty("LastUpdatedDate")]
        public string LastUpdatedDate { get; set; }

        [JsonProperty("Sha256Hex")]
        public string Sha256Hex { get; set; }

        [JsonProperty("OwnerOrganization")]
        public EntityHeader OwnerOrganization { get; set; }

        public SyncEntitySummary ToSummary()
        {
            return new SyncEntitySummary
            {
                Id = Id,
                EntityType = EntityType?.Trim(),
                Key = Key?.Trim(),
                Name = Name?.Trim(),
                Revision = Revision,
                RevisionTimeStamp = RevisionTimeStamp,
                ETag = ETag,
                IsDeleted = IsDeleted,
                IsDeprecated = IsDeprecated,
                IsDraft = IsDraft,
                LastUpdatedDate = LastUpdatedDate,
                Sha256Hex = Sha256Hex
            };
        }
    }
}