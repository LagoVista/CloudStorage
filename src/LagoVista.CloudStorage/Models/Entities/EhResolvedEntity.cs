using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class EhResolvedEntity
    {
        public bool UpdatedEntity { get; set; }
        public EntityBase Entity { get; set; }
        public IReadOnlyList<EntityHeaderNode> EntityHeaderNodes { get; set; }
        public IReadOnlyList<EntityHeaderNode> NotFoundEntityHeaderNodes { get; set; }
        public IReadOnlyList<ForeignKeyEdge> ForeignKeyEdges { get; set; }

    }
}
