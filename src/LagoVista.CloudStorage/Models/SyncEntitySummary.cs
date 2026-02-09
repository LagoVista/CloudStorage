using LagoVista.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class SyncEntitySummary
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

        // Cosmos uses "_etag" on the wire; other stores can map this as needed.
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

        [JsonIgnore]
        public string FormattedLastUpdatedDate
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LastUpdatedDate))
                    return "(missing)";

                // Parse ISO 8601 safely, treat as UTC, and normalize to UTC
                if (DateTimeOffset.TryParse(
                        LastUpdatedDate,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var parsed))
                {
                    var delta = DateTimeOffset.UtcNow - parsed;

                    if (delta.TotalSeconds < 60) return "A few seconds ago";
                    if (delta.TotalMinutes < 2) return "A minute ago";
                    if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} minutes ago";
                    if (delta.TotalHours < 2) return "An hour ago";
                    if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} hours ago";
                    if (delta.TotalDays < 2) return "A day ago";
                    if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} days ago";

                    // Past a week: compact absolute time (UTC)
                    return parsed.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "Z";
                }

                return LastUpdatedDate; // fallback if it’s some non-date string
            }
        }
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
