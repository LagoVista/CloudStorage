using LagoVista.CloudStorage.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class ApplicationDataConcurrencyTokenTests
    {
        [TestMethod]
        public void FromValue_PreservesOpaqueValueAndRejectsInvalidInput()
        {
            const string serialized = " opaque-provider-token:value/with+symbols== ";

            var token = ApplicationDataConcurrencyToken.FromValue(serialized);

            Assert.AreEqual(serialized, token.Value);
            Assert.AreEqual(serialized, token.ToString());
            Assert.AreEqual(token, ApplicationDataConcurrencyToken.FromValue(token.Value));
            Assert.ThrowsExactly<ArgumentException>(() => ApplicationDataConcurrencyToken.FromValue(null));
            Assert.ThrowsExactly<ArgumentException>(() => ApplicationDataConcurrencyToken.FromValue(String.Empty));
            Assert.ThrowsExactly<ArgumentException>(() => ApplicationDataConcurrencyToken.FromValue("   "));
        }

        [TestMethod]
        public void ReconstructedToken_PreservesConditionalCurrentAndStaleSemantics()
        {
            const string serializedCurrent = "revision-1";
            var gate = new ConditionalVersionGate(ApplicationDataConcurrencyToken.FromValue(serializedCurrent));

            var reconstructedCurrent = ApplicationDataConcurrencyToken.FromValue(serializedCurrent);
            Assert.AreEqual(ApplicationDataMutationStatus.Updated, gate.UpdateIfVersion(reconstructedCurrent));

            var reconstructedStale = ApplicationDataConcurrencyToken.FromValue(serializedCurrent);
            Assert.AreEqual(ApplicationDataMutationStatus.Conflict, gate.UpdateIfVersion(reconstructedStale));
        }

        private sealed class ConditionalVersionGate
        {
            private ApplicationDataConcurrencyToken _current;

            public ConditionalVersionGate(ApplicationDataConcurrencyToken current)
            {
                _current = current;
            }

            public ApplicationDataMutationStatus UpdateIfVersion(ApplicationDataConcurrencyToken expected)
            {
                if (!_current.Equals(expected))
                    return ApplicationDataMutationStatus.Conflict;

                _current = ApplicationDataConcurrencyToken.FromValue("revision-2");
                return ApplicationDataMutationStatus.Updated;
            }
        }
    }
}
