using Azure.Data.Tables;
using LagoVista.CloudStorage.Models;
using System;

namespace LagoVista.CloudStorage.Utils
{
    public static class NodeLocatorTableEntityFactory
    {
        public static TableEntity ToEntity(NodeLocatorEntry e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            e.Normalize();

            string pk, rk;
            NodeLocatorKeys.GetKeys(e.NodeId, out pk, out rk);

            var te = new TableEntity(pk, rk);

            te["NodeId"] = e.NodeId;
            te["NodePath"] = e.NodePath;
            te["NodePathHash"] = e.NodePathHash;
            te["NodeType"] = String.IsNullOrWhiteSpace(e.NodeType) ? "-" : e.NodeType;

            te["RootOrgId"] = String.IsNullOrWhiteSpace(e.RootOrgId) ? "-" : e.RootOrgId;
            te["RootType"] = String.IsNullOrWhiteSpace(e.RootType) ? "-" : e.RootType;
            te["RootId"] = String.IsNullOrWhiteSpace(e.RootId) ? "-" : e.RootId;

            te["SeenAt"] = e.SeenAt.UtcDateTime;

            if (e.RootRevision.HasValue)
                te["RootRevision"] = e.RootRevision.Value;

            if (!String.IsNullOrWhiteSpace(e.RootLastUpdatedDate))
                te["RootLastUpdatedDate"] = e.RootLastUpdatedDate;

            // Optional convenience for debugging
            te["Address"] = $"/{e.RootType}/{e.RootId}{(e.NodePath == "/" ? "" : e.NodePath)}";

            return te;
        }

        public static TableEntity ToDeleteEntity(string nodeId)
        {
            string pk, rk;
            NodeLocatorKeys.GetKeys(nodeId, out pk, out rk);

            // ETag.All lets us delete without fetching.
            var te = new TableEntity(pk, rk) { ETag = Azure.ETag.All };
            return te;
        }
    }
}
