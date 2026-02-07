using LagoVista.CloudStorage.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.CloudStorage.Storage
{
    public static class NodeLocatorWalker
    {
        // STRICT finite list of allowed EntityHeader keys (case-sensitive)
        private static readonly HashSet<string> EntityHeaderKeyAllowList =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Id",
                "Key",
                "Text",
                "_t",
                "Resolved",
                "IsPublic",
                "EntityType",
                "OwnerOrgId",
                "Path",
                "HasValue",
                "Value"
            };

        public static List<NodeLocatorEntry> ExtractNodeLocators(
            JObject rootDoc,
            string rootOrgId,
            string rootType,
            string rootId,
            int? rootRevision = null,
            string rootLastUpdatedDate = null)
        {
            if (rootDoc == null) throw new ArgumentNullException(nameof(rootDoc));
            if (String.IsNullOrWhiteSpace(rootId)) throw new ArgumentNullException(nameof(rootId));
            if (String.IsNullOrWhiteSpace(rootType)) throw new ArgumentNullException(nameof(rootType));

            var results = new List<NodeLocatorEntry>(capacity: 64);

            // Always include root itself exactly once
            results.Add(new NodeLocatorEntry
            {
                NodeId = rootId,
                NodePath = "/",
                NodeType = rootType,
                RootOrgId = rootOrgId,
                RootType = rootType,
                RootId = rootId,
                RootRevision = rootRevision,
                RootLastUpdatedDate = rootLastUpdatedDate
            });

            // Walk children of root at "/"
            WalkToken(rootDoc, "/", results, rootOrgId, rootType, rootId, rootRevision, rootLastUpdatedDate);

            return results;
        }


        private static readonly HashSet<string> SingularizationDoNotTouch =
            new HashSet<string>(StringComparer.Ordinal)
            {
                // Common "looks plural but isn't" or special nouns
                "Status",
                "News",
                "Series",
                "Species"
            };

        private static readonly Dictionary<string, string> SingularizationOverrides =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Domain-specific wins (add as you find them)
                // { "AgentModes", "AgentMode" },
                // { "ToolBoxes", "ToolBox" },
            };

        private static string SingularizeSegment(string segment)
        {
            if (String.IsNullOrWhiteSpace(segment)) return segment;

            // Overrides first
            string ov;
            if (SingularizationOverrides.TryGetValue(segment, out ov))
                return ov;

            // Don't touch known invariants
            if (SingularizationDoNotTouch.Contains(segment))
                return segment;

            // If it doesn't end with 's' or 'S', probably not plural
            var last = segment[segment.Length - 1];
            if (last != 's' && last != 'S')
                return segment;

            // Avoid breaking very short tokens (e.g., "Is", "As")
            if (segment.Length <= 3)
                return segment;

            // Apply a few high-value English rules (case-preserving-ish)
            // ies -> y (Companies -> Company)
            if (EndsWith(segment, "ies"))
                return segment.Substring(0, segment.Length - 3) + "y";

            // ...sses -> ...ss (Classes -> Class)
            if (EndsWith(segment, "sses"))
                return segment.Substring(0, segment.Length - 2);

            // ...ches/shes/xes/zes -> drop "es" (Watches -> Watch)
            if (EndsWith(segment, "ches") || EndsWith(segment, "shes") || EndsWith(segment, "xes") || EndsWith(segment, "zes"))
                return segment.Substring(0, segment.Length - 2);

            // ...ses -> ...s (Processes -> Process) BUT avoid "Cases"->"Cas" style issues?
            // This is still generally correct for your domain tokens.
            if (EndsWith(segment, "ses"))
                return segment.Substring(0, segment.Length - 2);

            // Default: drop trailing 's' (Agents -> Agent)
            return segment.Substring(0, segment.Length - 1);
        }

        private static bool EndsWith(string value, string suffix)
        {
            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }


        private static void WalkToken(
                JToken token,
                string path,
                List<NodeLocatorEntry> results,
                string rootOrgId,
                string rootType,
                string rootId,
                int? rootRevision,
                string rootLastUpdatedDate)
        {
            if (token == null) return;

            var obj = token as JObject;
            if (obj != null)
            {
                // Skip EntityHeaders entirely so we never index them as nodes
                if (IsEntityHeaderLike(obj))
                    return;


                // These code notes that point to each other in the same object
                // we'll never index against these from somewhere welse
                if ((path.Contains("Routes") && path.Contains("PipelineModules"))||
                    path.Contains("WidgetAttributes") ||
                    (path.Contains("Revisions") && path.Contains("Preprocessors")) ||
                    (rootType == "WorkTask" && (path.Contains("Issues") || path.Contains("ExpectedOutcomes") || path.Contains("HelpResources"))) ||
                    (rootType == "ServiceTicketTemplate" && (path.Contains("Tools") || path.Contains("PartsKits") || path.Contains("Instructions") || path.Contains("Resources")))
                    )
                    return;

                // Emit node locator for any non-EntityHeader object that has a normalized GUID id
                var nodeId = ReadString(obj, "id", "Id");
                if (!String.IsNullOrWhiteSpace(nodeId) && IsNormalizedGuid32(nodeId))
                {
                    // Root was already emitted explicitly
                    if (path != "/")
                    {
                        var explicitType = ReadString(obj, "EntityType");
                        var nodeType = !String.IsNullOrWhiteSpace(explicitType)
                            ? explicitType
                            : SingularizeSegment(DeriveNodeTypeFromPath(path));

                        results.Add(new NodeLocatorEntry
                        {
                            NodeId = nodeId,
                            NodePath = NormalizeRootPath(path),
                            NodeType = nodeType,

                            RootOrgId = rootOrgId,
                            RootType = rootType,
                            RootId = rootId,
                            RootRevision = rootRevision,
                            RootLastUpdatedDate = rootLastUpdatedDate
                        });
                    }
                }

                // Walk properties (object graph)
                foreach (var prop in obj.Properties())
                {
                    var childPath = Append(path, prop.Name);
                    WalkToken(prop.Value, childPath, results, rootOrgId, rootType, rootId, rootRevision, rootLastUpdatedDate);
                }

                return;
            }

            var arr = token as JArray;
            if (arr != null)
            {
                // Walk elements with stable id selector if possible: /Roles/@id=<GUID>
                for (int i = 0; i < arr.Count; i++)
                {
                    var child = arr[i];

                    var childObj = child as JObject;
                    if (childObj != null)
                    {
                        // If element is an EntityHeader object, skip it entirely
                        if (IsEntityHeaderLike(childObj))
                            continue;

                        // If it has a normalized GUID id, use stable segment
                        var childId = ReadString(childObj, "id", "Id");
                        if (!String.IsNullOrWhiteSpace(childId) && IsNormalizedGuid32(childId))
                        {
                            var stablePath = Append(path, "@id=" + childId);
                            WalkToken(child, stablePath, results, rootOrgId, rootType, rootId, rootRevision, rootLastUpdatedDate);
                            continue;
                        }
                    }

                    // Fallback: numeric index
                    var idxPath = Append(path, i.ToString());
                    WalkToken(child, idxPath, results, rootOrgId, rootType, rootId, rootRevision, rootLastUpdatedDate);
                }

                return;
            }

            // JValue: nothing to do
        }

        private static bool IsEntityHeaderLike(JObject obj)
        {
            // EntityHeader-like ONLY if every property is in the finite allowlist.
            // Case-sensitive match as requested.
            var props = obj.Properties().ToList();
            if (props.Count == 0) return false;

            foreach (var p in props)
            {
                if (!EntityHeaderKeyAllowList.Contains(p.Name))
                    return false;
            }

            return true;
        }

        private static string DeriveNodeTypeFromPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || path == "/") return "-";

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = parts.Length - 1; i >= 0; i--)
            {
                var seg = parts[i];

                // Skip selector segments
                if (seg.StartsWith("@id=", StringComparison.Ordinal)) continue;

                // Skip numeric index segments
                int idx;
                if (Int32.TryParse(seg, out idx)) continue;

                // Remaining segment is a property/container name
                return seg;
            }

            return "-";
        }


        private static string ReadString(JObject obj, params string[] names)
        {
            foreach (var name in names)
            {
                var t = obj[name];
                if (t == null) continue;

                var s = t.Type == JTokenType.Null ? null : t.Value<string>();
                if (!String.IsNullOrWhiteSpace(s)) return s;
            }

            return null;
        }

        private static string Append(string basePath, string segment)
        {
            if (String.IsNullOrEmpty(basePath) || basePath == "/")
                return "/" + segment;

            if (basePath.EndsWith("/", StringComparison.Ordinal))
                return basePath + segment;

            return basePath + "/" + segment;
        }

        private static string NormalizeRootPath(string path)
        {
            return String.IsNullOrWhiteSpace(path) ? "/" : path;
        }

        private static bool IsNormalizedGuid32(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;

            var s = value.Trim();
            if (s.Length != 32) return false;

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                var isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!isHex) return false;
            }

            return true;
        }
    }
}
