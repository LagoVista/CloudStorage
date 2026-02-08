using LagoVista.CloudStorage.Models;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Utils
{
    public static class NodeLocatorDiff
    {
        public sealed class DiffResult
        {
            public List<NodeLocatorEntry> Upserts { get; } = new List<NodeLocatorEntry>();
            public List<NodeLocatorEntry> Deletes { get; } = new List<NodeLocatorEntry>();
        }

        public static DiffResult Diff(IEnumerable<NodeLocatorEntry> oldEntries, IEnumerable<NodeLocatorEntry> newEntries)
        {
            var result = new DiffResult();

            var oldMap = new Dictionary<string, NodeLocatorEntry>(StringComparer.OrdinalIgnoreCase);
            if (oldEntries != null)
            {
                foreach (var e in oldEntries)
                {
                    if (e == null) continue;
                    e.Normalize();
                    if (String.IsNullOrWhiteSpace(e.NodeId)) continue;
                    oldMap[e.NodeId] = e;
                }
            }

            var newMap = new Dictionary<string, NodeLocatorEntry>(StringComparer.OrdinalIgnoreCase);
            if (newEntries != null)
            {
                foreach (var e in newEntries)
                {
                    if (e == null) continue;
                    e.Normalize();
                    if (String.IsNullOrWhiteSpace(e.NodeId)) continue;
                    newMap[e.NodeId] = e;
                }
            }

            // Upserts: anything in new (new or changed)
            foreach (var kvp in newMap)
            {
                result.Upserts.Add(kvp.Value);
            }

            // Deletes: anything that existed before but not now
            foreach (var kvp in oldMap)
            {
                if (!newMap.ContainsKey(kvp.Key))
                    result.Deletes.Add(kvp.Value);
            }

            return result;
        }
    }
}
