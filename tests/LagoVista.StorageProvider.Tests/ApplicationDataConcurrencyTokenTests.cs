using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Exceptions;
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
            var replacement = gate.UpdateIfVersion(reconstructedCurrent);
            Assert.AreEqual("revision-2", replacement.Value);

            var reconstructedStale = ApplicationDataConcurrencyToken.FromValue(serializedCurrent);
            var conflict = Assert.ThrowsExactly<RecordConflictException>(
                () => gate.UpdateIfVersion(reconstructedStale));
            Assert.AreEqual("revision-1", conflict.ExpectedVersion);
            Assert.AreEqual("revision-2", conflict.ActualVersion);
        }

        private sealed class ConditionalVersionGate
        {
            private ApplicationDataConcurrencyToken _current;

            public ConditionalVersionGate(ApplicationDataConcurrencyToken current)
            {
                _current = current;
            }

            public ApplicationDataConcurrencyToken UpdateIfVersion(ApplicationDataConcurrencyToken expected)
            {
                if (!_current.Equals(expected))
                    throw new RecordConflictException("TestRecord", "record-1", expected.Value, _current.Value);

                _current = ApplicationDataConcurrencyToken.FromValue("revision-2");
                return _current;
            }
        }
    }
}
