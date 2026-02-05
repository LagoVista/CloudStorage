using LagoVista.CloudStorage.Models;
using System.Collections.Generic;
using LagoVista.Core.Models;

namespace LagoVista.CloudStorage.Utils
{
    public static class EdgeFactory
    {
        public static IEnumerable<ForeignKeyEdge> FromHeaderNodes(
            EntityPk source,
            IEnumerable<EntityHeaderNode> nodes,
            int? sourceRevision = null,
            string sourceLastUpdatedDate = null)
        {
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                    continue;

                var target = new EntityPk(
                    source.OrgId,                   // assume same org
                    node.EntityType ?? "Unknown",
                    node.Id
                );

                yield return new ForeignKeyEdge
                {
                    Source = source,
                    Target = target,
                    RefPath = node.Path,
                    TargetKey = node.Key,
                    TargetText = node.Text,
                    SourceRevision = sourceRevision,
                    SourceLastUpdatedDate = sourceLastUpdatedDate,
                    SeenAt = System.DateTimeOffset.UtcNow
                };
            }
        }
    }
}

