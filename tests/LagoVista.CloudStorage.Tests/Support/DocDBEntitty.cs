using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Newtonsoft.Json;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class DocDBEntitty : IIDEntity, INoSQLEntity
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        public string? DatabaseName { get; set; }
        public string? EntityType { get; set; }

        public string? Name { get; set;  }

        public string? Description { get; set; }

        public int Index { get; set; }

        public EntityHeader? OwnerOrganization { get; set; }
    }
}
