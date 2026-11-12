using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class D3MDefinitionTests
    {
        private static D3MTypeRegistry Registry()
            => new D3MTypeRegistry()
                .Register(new D3MTypeRegistration("VTM", D3MStorageKind.EntityBase, allowTopLevelSelection: true)
                    .AddRelationship("AgentContext", "AgentContext", D3MCardinality.One)
                    .AddRelationship("EditorialImages", "Media", D3MCardinality.Many)
                    .AddRelationship("Department", "Department", D3MCardinality.One,
                        conditionKey: "has-department", supportsCompatibleStableTarget: true))
                .Register(new D3MTypeRegistration("AgentContext", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("Department", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("Media", D3MStorageKind.Media))
                .Register(new D3MTypeRegistration("AppRecord", D3MStorageKind.ApplicationData,
                    applicationDataCollection: "AppRecord"));

        private static D3MDefinitionRecord Valid()
            => new D3MDefinitionRecord
            {
                Id = D3MIdentity.Definition("VTM"),
                Organization = new EntityHeader { Id = "ORG-A" },
                TypeKey = "VTM",
                Name = "Virtual Team Member",
                StorageKind = D3MStorageKind.EntityBase,
                TopLevelSelectable = true,
                Dependencies = new List<D3MDependencyRule>
                {
                    new D3MDependencyRule
                    {
                        PropertyPath = "AgentContext",
                        TargetTypeKey = "AgentContext",
                        Cardinality = D3MCardinality.One,
                        Presence = D3MPresence.Required
                    },
                    new D3MDependencyRule
                    {
                        PropertyPath = "EditorialImages",
                        TargetTypeKey = "Media",
                        Cardinality = D3MCardinality.Many,
                        Presence = D3MPresence.Optional
                    }
                }
            };

        [TestMethod]
        public void DeterministicIds_AreValidAndRevisionSpecific()
        {
            var draftId = D3MIdentity.Definition("VTM");
            Assert.AreEqual(32, draftId.Value.Length);
            Assert.AreEqual(draftId, D3MIdentity.Definition("vtm"));
            Assert.AreNotEqual(draftId, D3MIdentity.Published("VTM", 1));
            Assert.AreNotEqual(D3MIdentity.Published("VTM", 1), D3MIdentity.Published("VTM", 2));
        }

        [TestMethod]
        public void ValidImmediateDependencies_AreAccepted()
        {
            var result = new D3MValidator(Registry()).Validate(Valid());
            Assert.IsTrue(result.IsValid, String.Join("; ", result.Issues));
        }

        [TestMethod]
        public void MissingMapIsNeverImplicitlyALeaf()
        {
            var draft = Valid();
            draft.Dependencies.Clear();
            var result = new D3MValidator(Registry()).Validate(draft);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(Has(result, "missing-explicit-leaf"));

            draft.ExplicitLeaf = true;
            Assert.IsTrue(new D3MValidator(Registry()).Validate(draft).IsValid);
        }

        [TestMethod]
        public void RegistryRejectsUnapprovedPropertyAndType()
        {
            var draft = Valid();
            draft.Dependencies[0].PropertyPath = "UnknownProperty";
            var result = new D3MValidator(Registry()).Validate(draft);
            Assert.IsTrue(Has(result, "unknown-path"));

            draft = Valid();
            draft.Dependencies[0].TargetTypeKey = "UnknownClrType";
            result = new D3MValidator(Registry()).Validate(draft);
            Assert.IsTrue(Has(result, "unknown-target"));
            Assert.IsTrue(Has(result, "target-mismatch"));
        }

        [TestMethod]
        public void OptionalPopulatedReferenceMustHaveKnownHandler()
        {
            var draft = Valid();
            draft.Dependencies[1].TargetTypeKey = "UndefinedMedia";
            Assert.IsTrue(Has(new D3MValidator(Registry()).Validate(draft), "unknown-target"));
        }

        [TestMethod]
        public void ConditionalAndTargetCompatibilityRequireRegisteredHandler()
        {
            var draft = Valid();
            draft.Dependencies.Add(new D3MDependencyRule
            {
                PropertyPath = "Department",
                TargetTypeKey = "Department",
                Presence = D3MPresence.Conditional,
                ConditionKey = "has-department",
                Cardinality = D3MCardinality.One,
                VersionRequirement = D3MVersionRequirement.CompatibleStableTarget
            });
            Assert.IsTrue(new D3MValidator(Registry()).Validate(draft).IsValid);

            draft.Dependencies[2].ConditionKey = "untrusted-expression";
            Assert.IsTrue(Has(new D3MValidator(Registry()).Validate(draft), "unregistered-condition"));

            draft.Dependencies[2].ConditionKey = "has-department";
            draft.Dependencies[0].VersionRequirement = D3MVersionRequirement.CompatibleStableTarget;
            Assert.IsTrue(Has(new D3MValidator(Registry()).Validate(draft), "unsupported-compatibility"));
        }

        [TestMethod]
        public void ApplicationDataCollectionMustMatchCodeRegistration()
        {
            var draft = new D3MDefinitionRecord
            {
                Id = D3MIdentity.Definition("AppRecord"),
                Organization = new EntityHeader { Id = "ORG-A" },
                TypeKey = "AppRecord",
                Name = "Example",
                StorageKind = D3MStorageKind.ApplicationData,
                ApplicationDataCollection = "WrongCollection",
                ExplicitLeaf = true
            };
            var result = new D3MValidator(Registry()).Validate(draft);
            Assert.IsTrue(Has(result, "collection-mismatch"));
            draft.ApplicationDataCollection = "AppRecord";
            Assert.IsTrue(new D3MValidator(Registry()).Validate(draft).IsValid);
        }

        [TestMethod]
        public void DraftCannotChangeRegisteredTransferKindOrBecomeUnauthorizedRoot()
        {
            var draft = Valid();
            draft.StorageKind = D3MStorageKind.Product;
            Assert.IsTrue(Has(new D3MValidator(Registry()).Validate(draft), "storage-mismatch"));
            draft = Valid();
            draft.TypeKey = "AgentContext";
            draft.TopLevelSelectable = true;
            Assert.IsTrue(Has(new D3MValidator(Registry()).Validate(draft), "root-not-allowed"));
        }

        [TestMethod]
        public async Task PublishInsertsPinnedSnapshotThenConditionallyAdvancesPointer()
        {
            var draft = Valid();
            var original = new ApplicationDataConcurrencyToken("draft-1");
            var stored = new VersionedApplicationDataRecord<D3MDefinitionRecord>(draft, original);
            var store = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            D3MPublishedDefinitionRecord inserted = null;
            store.Setup(s => s.GetVersionedAsync<D3MDefinitionRecord>(
                    It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored);
            store.Setup(s => s.InsertAsync(It.IsAny<D3MPublishedDefinitionRecord>(), It.IsAny<CancellationToken>()))
                .Callback<D3MPublishedDefinitionRecord, CancellationToken>((record, _) => inserted = record)
                .Returns(Task.CompletedTask);
            store.Setup(s => s.UpdateIfVersionAsync(draft, original, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationDataConcurrencyToken("draft-2"));

            var service = new D3MDefinitionService(store.Object, Registry());
            var published = await service.PublishAsync("VTM", "ORG-A", "operator", original);

            Assert.AreSame(inserted, published);
            Assert.AreEqual(1, published.Revision);
            Assert.AreEqual(1, draft.PublishedRevision);
            Assert.AreEqual("operator", published.PublishedBy);
            Assert.AreEqual(D3MIdentity.Published("VTM", 1), published.Id);
            draft.Dependencies[0].TargetTypeKey = "ModifiedDraft";
            Assert.AreEqual("AgentContext", published.Dependencies[0].TargetTypeKey,
                "The immutable publication cannot alias mutable draft dependency instances.");
            store.VerifyAll();
        }

        [TestMethod]
        public async Task UnpublishedDraftIsNeverReturnedAsPublished()
        {
            var draft = Valid();
            var store = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            store.Setup(s => s.GetVersionedAsync<D3MDefinitionRecord>(
                    It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VersionedApplicationDataRecord<D3MDefinitionRecord>(
                    draft, new ApplicationDataConcurrencyToken("draft-1")));

            var service = new D3MDefinitionService(store.Object, Registry());
            Assert.IsNull(await service.GetPublishedAsync("VTM", "ORG-A"));
            store.VerifyAll();
        }

        [TestMethod]
        public async Task MissingPublishedRevisionFailsClosed()
        {
            var draft = Valid();
            draft.PublishedRevision = 1;
            var store = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            store.Setup(s => s.GetVersionedAsync<D3MDefinitionRecord>(
                    It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VersionedApplicationDataRecord<D3MDefinitionRecord>(
                    draft, new ApplicationDataConcurrencyToken("draft-1")));
            store.Setup(s => s.GetAsync<D3MPublishedDefinitionRecord>(
                    It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((D3MPublishedDefinitionRecord)null);
            var service = new D3MDefinitionService(store.Object, Registry());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await service.GetPublishedAsync("VTM", "ORG-A"));
            store.VerifyAll();
        }

        [TestMethod]
        public async Task DraftUpdateCannotOverwritePublicationProvenance()
        {
            var draft = Valid();
            draft.DraftRevision = 2;
            draft.PublishedRevision = 1;
            draft.PublishedBy = "approver";
            var token = new ApplicationDataConcurrencyToken("draft-2");
            var proposed = Valid();
            proposed.Name = "New title";
            proposed.PublishedRevision = 99;
            proposed.PublishedBy = "untrusted";
            var store = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            store.Setup(s => s.GetVersionedAsync<D3MDefinitionRecord>(
                    It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VersionedApplicationDataRecord<D3MDefinitionRecord>(draft, token));
            store.Setup(s => s.UpdateIfVersionAsync(draft, token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationDataConcurrencyToken("draft-3"));

            await new D3MDefinitionService(store.Object, Registry()).UpdateDraftIfVersionAsync(proposed, token);

            Assert.AreEqual(3, draft.DraftRevision);
            Assert.AreEqual(1, draft.PublishedRevision);
            Assert.AreEqual("approver", draft.PublishedBy);
            Assert.AreEqual("New title", draft.Name);
            store.VerifyAll();
        }

        private static bool Has(D3MValidationResult result, string code)
        {
            foreach (var issue in result.Issues)
                if (issue.Code == code) return true;
            return false;
        }
    }
}
