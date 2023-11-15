using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class DocDBEntitty : IIDEntity, INamedEntity, IKeyedEntity, IOwnedEntity, INoSQLEntity
    {
        [JsonProperty("id")]
        public string? Id { get; set; } = Guid.NewGuid().ToId();

        public string? DatabaseName { get; set; }
        public string? EntityType { get; set; }

        public string? Name { get; set;  }

        public string? Description { get; set; }

        public int Index { get; set; }

        public EntityHeader? OwnerOrganization { get; set; }
        public bool IsPublic { get; set; }
        public EntityHeader OwnerUser { get; set; }
        public string Key { get; set;}
    }
}
