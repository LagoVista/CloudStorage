using LagoVista.CloudStorage.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class StorageRetentionPolicyTests
    {
        [TestMethod]
        public void Resolve_UsesScopedOverrideBeforeRecordClassDefault()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, TimeSpan.FromDays(30), summarizeBeforeExpiry: true)
                .AddOverride(StorageRecordClass.ActivityRecord, "ORG-A", TimeSpan.FromDays(7));

            var scoped = policy.Resolve(StorageRecordClass.ActivityRecord, "ORG-A");
            var fallback = policy.Resolve(StorageRecordClass.ActivityRecord, "ORG-B");

            Assert.AreEqual(TimeSpan.FromDays(7), scoped.EffectiveTtl);
            Assert.AreEqual("scoped-override", scoped.Source);
            Assert.AreEqual(TimeSpan.FromDays(30), fallback.EffectiveTtl);
            Assert.IsTrue(fallback.SummarizeBeforeExpiry);
        }

        [TestMethod]
        public void Resolve_ProtectedOverrideIsNonExpiring()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.Scratch, TimeSpan.FromHours(24))
                .AddOverride(StorageRecordClass.Scratch, "LEGAL-HOLD", isProtected: true, summarizeBeforeExpiry: true);

            var decision = policy.Resolve(StorageRecordClass.Scratch, "LEGAL-HOLD");

            Assert.IsTrue(decision.IsProtected);
            Assert.IsNull(decision.EffectiveTtl);
            Assert.IsTrue(decision.SummarizeBeforeExpiry);
            Assert.IsTrue(decision.ExplicitlyGoverned);
        }

        [TestMethod]
        public void Resolve_UnconfiguredApplicationDataIsDurable()
        {
            var decision = new StorageRetentionPolicy().Resolve(StorageRecordClass.ApplicationData, "ORG-A");

            Assert.IsNull(decision.EffectiveTtl);
            Assert.IsFalse(decision.ExplicitlyGoverned);
            Assert.IsFalse(decision.IsProtected);
        }

        [TestMethod]
        public void PolicyRejectsProtectedRuleWithTtl()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                new StorageRetentionPolicy().AddDefault(
                    StorageRecordClass.ActivityRecord,
                    TimeSpan.FromDays(1),
                    isProtected: true));
        }
    }
}
