using LagoVista.CloudStorage.Storage;
using NUnit.Framework;

namespace LagoVista.CloudStorage.Tests
{
    public class StorageRecordIdentityTests
    {
        private sealed class DefaultNamedRecord { }

        [StorageRecordName("AgentSession")]
        private sealed class AgentSessionStorageRecord { }

        [Test]
        public void GetCollectionName_UsesClrTypeNameByDefault()
        {
            Assert.That(StorageRecordIdentity.GetCollectionName<DefaultNamedRecord>(), Is.EqualTo(nameof(DefaultNamedRecord)));
        }

        [Test]
        public void GetCollectionName_UsesCanonicalStorageRecordNameWhenDeclared()
        {
            Assert.That(StorageRecordIdentity.GetCollectionName<AgentSessionStorageRecord>(), Is.EqualTo("AgentSession"));
        }
    }
}
