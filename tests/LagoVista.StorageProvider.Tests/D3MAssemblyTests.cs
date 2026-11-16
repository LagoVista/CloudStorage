using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class D3MAssemblyTests
    {
        private const string Org = "ORG-A";

        private sealed class PublishedCatalog : ID3MPublishedDefinitionReader
        {
            public readonly Dictionary<string, D3MPublishedDefinitionRecord> Maps =
                new Dictionary<string, D3MPublishedDefinitionRecord>(StringComparer.Ordinal);
            public Task<D3MPublishedDefinitionRecord> GetPublishedAsync(
                string typeKey, string organizationId, CancellationToken cancellationToken = default)
            {
                Assert.AreEqual(Org, organizationId);
                Maps.TryGetValue(typeKey, out var map);
                return Task.FromResult(map);
            }
        }

        private sealed class Adapter : ID3MApprovedSourceAdapter
        {
            public D3MStorageKind Kind { get; }
            public bool IncludeConditional { get; set; } = true;
            public readonly Dictionary<string, D3MApprovedSourceRecord> Records =
                new Dictionary<string, D3MApprovedSourceRecord>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, int> ReadCounts =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public Adapter(D3MStorageKind kind) { Kind = kind; }

            public Adapter Add(string type, string id, bool approved = true, string organizationId = Org)
            {
                Records[type + ":" + id] = new D3MApprovedSourceRecord
                {
                    OrganizationId = organizationId, TypeKey = type, Id = id, Key = id,
                    Name = type + " " + id, StableRevision = "approved:" + id,
                    IsApproved = approved,
                    Snapshot = new Dictionary<string, List<D3MSourceReference>>(StringComparer.Ordinal)
                };
                return this;
            }

            public Adapter Link(string ownerType, string ownerId, string path, params string[] ids)
            {
                var source = Records[ownerType + ":" + ownerId];
                var paths = (Dictionary<string, List<D3MSourceReference>>)source.Snapshot;
                paths[path] = ids.Select(id => new D3MSourceReference { Id = id, Key = id }).ToList();
                return this;
            }

            public Task<D3MApprovedSourceRecord> ReadApprovedAsync(
                D3MRecordLocator locator, D3MTypeRegistration registration,
                CancellationToken cancellationToken = default)
            {
                var id = locator.Id ?? locator.Key;
                var key = locator.TypeKey + ":" + id;
                ReadCounts[key] = ReadCounts.TryGetValue(key, out var count) ? count + 1 : 1;
                Records.TryGetValue(key, out var record);
                return Task.FromResult(record);
            }

            public Task<IReadOnlyList<D3MSourceReference>> ReadReferencesAsync(
                D3MApprovedSourceRecord source, D3MRegisteredRelationship relationship,
                CancellationToken cancellationToken = default)
            {
                var paths = (Dictionary<string, List<D3MSourceReference>>)source.Snapshot;
                paths.TryGetValue(relationship.PropertyPath, out var references);
                return Task.FromResult((IReadOnlyList<D3MSourceReference>)
                    (references ?? new List<D3MSourceReference>()));
            }

            public Task<bool> EvaluateConditionAsync(
                D3MApprovedSourceRecord source, D3MRegisteredRelationship relationship,
                CancellationToken cancellationToken = default)
                => Task.FromResult(IncludeConditional);
        }

        private static D3MDependencyRule Rule(string property, string target,
            D3MCardinality cardinality, D3MPresence presence = D3MPresence.Required,
            D3MRelationshipRole role = D3MRelationshipRole.Prerequisite, string conditionKey = null)
            => new D3MDependencyRule
            {
                PropertyPath = property, TargetTypeKey = target, Cardinality = cardinality,
                Presence = presence, Role = role, ConditionKey = conditionKey
            };

        private static D3MPublishedDefinitionRecord Published(string type,
            D3MStorageKind kind, bool topLevel = false, string collection = null,
            params D3MDependencyRule[] dependencies)
            => new D3MPublishedDefinitionRecord
            {
                Id = D3MIdentity.Published(type, 4),
                Organization = new EntityHeader { Id = Org },
                TypeKey = type, Name = type, StorageKind = kind,
                ApplicationDataCollection = collection, SchemaVersion = 1, Revision = 4,
                PublishedBy = "approved-operator", PublishedUtc = DateTime.UtcNow,
                TopLevelSelectable = topLevel,
                ExplicitLeaf = dependencies.Length == 0,
                Dependencies = dependencies.ToList()
            };

        private static D3MTypeRegistry Registry()
            => new D3MTypeRegistry()
                .Register(new D3MTypeRegistration("VTM", D3MStorageKind.EntityBase, allowTopLevelSelection: true)
                    .AddRelationship("Department", "Department", D3MCardinality.One)
                    .AddRelationship("Contexts", "AppContext", D3MCardinality.Many)
                    .AddRelationship("AgentPersona", "Persona", D3MCardinality.One)
                    .AddRelationship("DefaultProduct", "Product", D3MCardinality.One))
                .Register(new D3MTypeRegistration("Department", D3MStorageKind.EntityBase)
                    .AddRelationship("Members", "VTM", D3MCardinality.Many,
                        D3MRelationshipRole.NavigationOnly))
                .Register(new D3MTypeRegistration("Persona", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("AppContext", D3MStorageKind.ApplicationData,
                    applicationDataCollection: "app-contexts")
                    .AddRelationship("Media", "Media", D3MCardinality.One))
                .Register(new D3MTypeRegistration("Media", D3MStorageKind.Media))
                .Register(new D3MTypeRegistration("Product", D3MStorageKind.Product));

        private static PublishedCatalog Catalog()
        {
            var catalog = new PublishedCatalog();
            catalog.Maps["VTM"] = Published("VTM", D3MStorageKind.EntityBase, true, null,
                Rule("Department", "Department", D3MCardinality.One),
                Rule("Contexts", "AppContext", D3MCardinality.Many, D3MPresence.Optional),
                Rule("AgentPersona", "Persona", D3MCardinality.One, D3MPresence.Optional),
                Rule("DefaultProduct", "Product", D3MCardinality.One, D3MPresence.Optional));
            catalog.Maps["Department"] = Published("Department", D3MStorageKind.EntityBase, false, null,
                Rule("Members", "VTM", D3MCardinality.Many,
                    D3MPresence.Optional, D3MRelationshipRole.NavigationOnly));
            catalog.Maps["Persona"] = Published("Persona", D3MStorageKind.EntityBase);
            catalog.Maps["AppContext"] = Published("AppContext", D3MStorageKind.ApplicationData,
                false, "app-contexts",
                Rule("Media", "Media", D3MCardinality.One));
            catalog.Maps["Media"] = Published("Media", D3MStorageKind.Media);
            catalog.Maps["Product"] = Published("Product", D3MStorageKind.Product);
            return catalog;
        }

        [TestMethod]
        public async Task MixedEntityBaseApplicationDataMediaAndProduct_ProducesPinnedDeduplicatedGraph()
        {
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1")
                .Link("VTM", "V1", "Department", "D1")
                .Link("VTM", "V1", "Contexts", "C2", "C1")
                .Link("VTM", "V1", "DefaultProduct", "P1")
                .Link("Department", "D1", "Members", "V1");
            var apps = new Adapter(D3MStorageKind.ApplicationData)
                .Add("AppContext", "C1").Add("AppContext", "C2")
                .Link("AppContext", "C1", "Media", "IMG1")
                .Link("AppContext", "C2", "Media", "IMG1");
            var media = new Adapter(D3MStorageKind.Media).Add("Media", "IMG1");
            var products = new Adapter(D3MStorageKind.Product).Add("Product", "P1");
            var result = await new D3MAssemblyService(Catalog(), Registry(),
                new ID3MApprovedSourceAdapter[] { entity, apps, media, products })
                .AssembleAsync(Org, "VTM", "V1");

            Assert.IsTrue(result.GraphComplete, String.Join("; ", result.Blockers));
            Assert.AreEqual(6, result.OrderedNodes.Count);
            Assert.AreEqual(7, result.Edges.Count);
            Assert.AreEqual(1, result.OrderedNodes.Count(n => n.TypeKey == "Media"));
            Assert.AreEqual(1, media.ReadCounts["Media:IMG1"]);
            Assert.AreEqual("VTM", result.OrderedNodes.Last().TypeKey);
            Assert.IsTrue(result.OrderedNodes.FindIndex(n => n.Id == "IMG1") <
                          result.OrderedNodes.FindIndex(n => n.Id == "C1"));
            Assert.IsTrue(result.Edges.Any(e => e.Role == D3MRelationshipRole.NavigationOnly &&
                                                e.FromTypeKey == "Department"));
            var app = result.OrderedNodes.Single(n => n.TypeKey == "AppContext" && n.Id == "C1");
            Assert.AreEqual(D3MStorageKind.ApplicationData, app.StorageKind);
            Assert.AreEqual("app-contexts", app.ApplicationDataCollection);
            Assert.AreEqual("approved:C1", app.StableSourceRevision);
            Assert.AreEqual(4, app.PublishedMapRevision);
            Assert.AreEqual(D3MIdentity.Published("AppContext", 4).Value, app.PublishedMapId);
            Assert.IsFalse(result.OrderedNodes.Any(n => n.TypeKey == "Persona"),
                "Absent optional persona must not become a prerequisite.");
        }

        [TestMethod]
        public async Task MissingPublishedMapBlocks_AndDoesNotTreatReferencedTypeAsLeaf()
        {
            var catalog = Catalog();
            catalog.Maps.Remove("AppContext");
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1")
                .Link("VTM", "V1", "Department", "D1")
                .Link("VTM", "V1", "Contexts", "C1");
            var apps = new Adapter(D3MStorageKind.ApplicationData).Add("AppContext", "C1");
            var result = await new D3MAssemblyService(catalog, Registry(),
                new ID3MApprovedSourceAdapter[] { entity, apps })
                .AssembleAsync(Org, "VTM", "V1");

            Assert.IsFalse(result.GraphComplete);
            Assert.IsTrue(result.Blockers.Any(b => b.Contains("Missing active published D3M for AppContext")));
        }

        [TestMethod]
        public async Task OptionalPopulatedReferenceWithoutApprovedSnapshotBlocks()
        {
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1").Add("Persona", "P1", approved: false)
                .Link("VTM", "V1", "Department", "D1")
                .Link("VTM", "V1", "AgentPersona", "P1");
            var result = await new D3MAssemblyService(Catalog(), Registry(),
                new ID3MApprovedSourceAdapter[] { entity })
                .AssembleAsync(Org, "VTM", "V1");

            Assert.IsFalse(result.GraphComplete);
            Assert.IsTrue(result.Blockers.Any(b => b.Contains("unapproved stable source snapshot for Persona:P1")));
            Assert.IsFalse(result.OrderedNodes.Any(n => n.TypeKey == "Persona"));
        }

        [TestMethod]
        public async Task CrossOrganizationRecordIsBlocked()
        {
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1", organizationId: "OTHER")
                .Link("VTM", "V1", "Department", "D1");
            var result = await new D3MAssemblyService(Catalog(), Registry(),
                new ID3MApprovedSourceAdapter[] { entity })
                .AssembleAsync(Org, "VTM", "V1");

            Assert.IsFalse(result.GraphComplete);
            Assert.IsTrue(result.Blockers.Any(b => b.Contains("Department:D1")));
        }

        [TestMethod]
        public async Task PrerequisiteCycleBlocks_ButReverseNavigationDoesNot()
        {
            var registry = Registry();
            // The VTM -> Department edge is a prerequisite, Department -> VTM is navigation only.
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1")
                .Link("VTM", "V1", "Department", "D1")
                .Link("Department", "D1", "Members", "V1");
            var catalog = Catalog();
            var normal = await new D3MAssemblyService(catalog, registry,
                new ID3MApprovedSourceAdapter[] { entity }).AssembleAsync(Org, "VTM", "V1");
            Assert.IsTrue(normal.GraphComplete);

            // Deliberate prerequisite self-cycle through the permitted VTM Department path.
            catalog.Maps["Department"] = Published("Department", D3MStorageKind.EntityBase, false, null,
                Rule("Members", "VTM", D3MCardinality.Many,
                    D3MPresence.Required, D3MRelationshipRole.NavigationOnly));
            // Trusted registration forbids promoting a navigation edge to a prerequisite.
            catalog.Maps["Department"].Dependencies[0].Role = D3MRelationshipRole.Prerequisite;
            var invalid = await new D3MAssemblyService(catalog, registry,
                new ID3MApprovedSourceAdapter[] { entity }).AssembleAsync(Org, "VTM", "V1");
            Assert.IsFalse(invalid.GraphComplete);
            Assert.IsTrue(invalid.Blockers.Any(b => b.Contains("Published D3M differs")));
        }

        [TestMethod]
        public async Task MissingApprovedRootAndMissingAdapterBothFailClosed()
        {
            var entity = new Adapter(D3MStorageKind.EntityBase)
                .Add("VTM", "V1").Add("Department", "D1")
                .Link("VTM", "V1", "Department", "D1")
                .Link("VTM", "V1", "Contexts", "C1");
            var missingAdapter = await new D3MAssemblyService(Catalog(), Registry(),
                new ID3MApprovedSourceAdapter[] { entity }).AssembleAsync(Org, "VTM", "V1");
            Assert.IsFalse(missingAdapter.GraphComplete);
            Assert.IsTrue(missingAdapter.Blockers.Any(b => b.Contains("No approved source adapter")));

            entity.Records["VTM:V1"].IsApproved = false;
            var unapproved = await new D3MAssemblyService(Catalog(), Registry(),
                new ID3MApprovedSourceAdapter[] { entity }).AssembleAsync(Org, "VTM", "V1");
            Assert.IsFalse(unapproved.GraphComplete);
            Assert.AreEqual(0, unapproved.OrderedNodes.Count);
        }
    }
}
