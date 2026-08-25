using System;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public static class NodeLocatorKeys
    {
        public const string NodeLocatorTable = "NodeLocator";

        /// <summary>
        /// PK = first 4 chars of node id (lower), RK = full id (lower).
        /// </summary>
        public static void GetKeys(string nodeId, out string pk, out string rk)
        {
            if (String.IsNullOrWhiteSpace(nodeId)) throw new ArgumentNullException(nameof(nodeId));

            var norm = nodeId.Trim();
            if (norm.Length < 4) throw new ArgumentException("nodeId must be at least 4 characters.", nameof(nodeId));

            pk = norm.Substring(0, 4);
            rk = norm;
        }

        public static string NormalizeId(string id)
        {
            return String.IsNullOrWhiteSpace(id) ? null : id.Trim();
        }

        public static string ComputePathHash16(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(path));
                return BitConverter.ToString(bytes, 0, 8).Replace("-", "");
            }
        }
    }

    /// <summary>
    /// Maps NodeId -> (Root + Path). Root nodes use NodePath = "/".
    /// </summary>
    public sealed class NodeLocatorEntry
    {
        public string NodeId { get; set; }              // the id you are resolving
        public string NodeName { get; set; }
        public string NodeKey { get; set; }
        public string NodeEntityType { get; set; }

        public string NodePath { get; set; }            // canonical /a/b/0/c ("/" means root)
        public string NodeType { get; set; }            // optional (if node has EntityType)
        public string NodePathHash { get; set; }        // optional convenience

        public string RootOrgId { get; set; }
        public string RootType { get; set; }
        public string RootId { get; set; }

        public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;

        // Optional root metadata for diagnostics
        public int? RootRevision { get; set; }
        public string RootLastUpdatedDate { get; set; }

        public void Normalize()
        {
            NodeId = NodeLocatorKeys.NormalizeId(NodeId);
            RootId = NodeLocatorKeys.NormalizeId(RootId);
            RootOrgId = NodeLocatorKeys.NormalizeId(RootOrgId);

            if (String.IsNullOrWhiteSpace(NodePath))
                NodePath = "/";

            if (String.IsNullOrWhiteSpace(NodePathHash))
                NodePathHash = NodeLocatorKeys.ComputePathHash16(NodePath);
        }
    }
}
