using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Models
{
    public class EntityListItem
    {
        [JsonProperty("id")]
        public NormalizedId32 Id { get; set; }

        public LagoVistaIcon Icon { get; set; }

        public string Name { get; set; }

        public LagoVistaKey Key { get; set; }

        public bool IsPublic { get; set; }

        public bool IsDraft { get; set; }

        public bool? IsDeleted { get; set; }

        public string Category { get; set; }

        public double? Stars { get; set; }

        public int RatingsCount { get; set; }

        public List<Label> Labels { get; set; }

        public EntityHeader Status { get; set; }
    }
}
