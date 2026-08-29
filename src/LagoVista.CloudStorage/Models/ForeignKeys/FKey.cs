using LagoVista.Core.AI.Models;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public sealed class EntityPk
    {
        public EntityPk(string orgId, string entityType, string entityId)
        {
            OrgId = orgId;
            EntityType = entityType;
            EntityId = entityId;
        }

        public string OrgId { get; }
        public string EntityType { get; }
        public string EntityId { get; }

        public string ToKey()
        {
            return $"{OrgId}|{EntityType}|{EntityId}";
        }
    }


    public sealed class ForeignKeyEdge
    {
        public EntityPk Source { get; set; }
        public EntityPk Target { get; set; }

        /// <summary>
        /// Canonical path like /devices/0/ownerOrganization
        /// </summary>
        public string RefPath { get; set; }

        /// <summary>
        /// Short stable hash of RefPath for RowKey usage
        /// </summary>
        public string RefPathHash { get; set; }

        // Optional metadata (does NOT affect identity)
        public string TargetKey { get; set; }
        public string TargetText { get; set; }

        public int? SourceRevision { get; set; }
        public string SourceLastUpdatedDate { get; set; }
        public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Identity for outbound diffing (source implied).
        /// </summary>
        public string GetOutboundIdentityKey()
        {
            return $"{Target.EntityId}|{RefPath}";
        }

        public void EnsurePathHash()
        {
            if (!string.IsNullOrEmpty(RefPathHash)) return;
            RefPathHash = ComputePathHash16(RefPath);
        }

        private static string ComputePathHash16(string path)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(path));
                return BitConverter.ToString(bytes, 0, 8)
                    .Replace("-", "")
                    .ToLowerInvariant();
            }
        }
    }

    public static class FkIndexKeys
    {
        public const string InboundTable = "FkInbound";
        public const string OutboundTable = "FkOutbound";
        public const string OrphandedTable = "FkOphaned";

        // Inbound: target → sources
        public static void GetInboundKeys(
            ForeignKeyEdge edge,
            out string partitionKey,
            out string rowKey)
        {
            edge.EnsurePathHash();

            partitionKey =
                $"{edge.Target.OrgId}|{edge.Target.EntityType}|{edge.Target.EntityId}";

            rowKey =
                $"{edge.Source.OrgId}|{edge.Source.EntityType}|{edge.Source.EntityId}|{edge.RefPathHash}";
        }

        // Outbound: source → targets
        public static void GetOutboundKeys(
            ForeignKeyEdge edge,
            out string partitionKey,
            out string rowKey)
        {
            edge.EnsurePathHash();

            partitionKey =
                $"{edge.Source.OrgId}|{edge.Source.EntityType}|{edge.Source.EntityId}";

            rowKey =
                $"{edge.Target.OrgId}|{edge.Target.EntityType}|{edge.Target.EntityId}|{edge.RefPathHash}";
        }
    }


    public static class FkEdgeDiff
    {
        public sealed class DiffResult
        {
            public List<ForeignKeyEdge> Added { get; } = new List<ForeignKeyEdge>();
            public List<ForeignKeyEdge> Removed { get; } = new List<ForeignKeyEdge>();
            public List<ForeignKeyEdge> SameIdentity { get; } = new List<ForeignKeyEdge>();
        }

        public static DiffResult DiffOutboundEdges(
            IEnumerable<ForeignKeyEdge> oldEdges,
            IEnumerable<ForeignKeyEdge> newEdges)
        {
            var result = new DiffResult();

            var oldMap = new Dictionary<string, ForeignKeyEdge>();
            foreach (var e in oldEdges)
            {
                e.EnsurePathHash();
                oldMap[e.GetOutboundIdentityKey()] = e;
            }

            var newMap = new Dictionary<string, ForeignKeyEdge>();
            foreach (var e in newEdges)
            {
                e.EnsurePathHash();
                newMap[e.GetOutboundIdentityKey()] = e;
            }

            foreach (var kvp in newMap)
            {
                if (!oldMap.ContainsKey(kvp.Key))
                    result.Added.Add(kvp.Value);
                else
                    result.SameIdentity.Add(kvp.Value);
            }

            foreach (var kvp in oldMap)
            {
                if (!newMap.ContainsKey(kvp.Key))
                    result.Removed.Add(kvp.Value);
            }

            return result;
        }
    }

    public static class ForeignKeyEdgeFactory
    {
        /// <summary>
        /// Converts discovered EntityHeaderNodes into FK edges for a single source entity.
        /// </summary>
        public static IEnumerable<ForeignKeyEdge> FromEntityHeaderNodes(
            IEntityBase source,
            IEnumerable<EntityHeaderNode> nodes)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (nodes == null) yield break;

            foreach (var node in nodes)
            {
                // FK must have a target id
                if (string.IsNullOrWhiteSpace(node.Id))
                    continue;

                // Default assumption: references stay in the same org
                var target = new EntityPk(
                    source.OwnerOrganization?.Id ?? "SYSTEM",
                    node.EntityType ?? "Unknown",
                    node.Id
                );

                var edge = new ForeignKeyEdge
                {
                    Source = new EntityPk(source.OwnerOrganization?.Id ?? "SYSTEM", source.EntityType, source.Id),
                    Target = target,

                    // Canonical path like /devices/0/ownerOrganization
                    RefPath = node.NormalizedPath,

                    // Optional metadata (NOT identity)
                    TargetKey = node.Key,
                    TargetText = node.Text,

                    SourceRevision = source.Revision,
                    SourceLastUpdatedDate = source.LastUpdatedDate,
                    SeenAt = DateTimeOffset.UtcNow
                };

                edge.EnsurePathHash();

                yield return edge;
            }
        }
    }

}
