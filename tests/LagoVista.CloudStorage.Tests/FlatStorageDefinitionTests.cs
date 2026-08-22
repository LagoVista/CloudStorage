using LagoVista.CloudStorage.Storage;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class FlatStorageDefinitionTests
    {
        private class LogRecord
        {
            public string OrganizationId { get; set; }
            public string DeviceId { get; set; }
            public DateTime Timestamp { get; set; }
            public string MessageType { get; set; }
        }

        [Test]
        public void Configure_WithPartitionTimeIndexAndRetention_CapturesLogicalShape()
        {
            var definition = new FlatStorageDefinition<LogRecord>()
                .PartitionBy(x => x.OrganizationId)
                .PartitionBy(x => x.DeviceId)
                .TimeBy(x => x.Timestamp)
                .BucketBy(StoragePeriod.Month)
                .Index(x => x.MessageType)
                .RetainFor(TimeSpan.FromDays(90));

            Assert.That(definition.PartitionFields, Is.EqualTo(new[] { "OrganizationId", "DeviceId" }));
            Assert.That(definition.TimeField, Is.EqualTo("Timestamp"));
            Assert.That(definition.BucketPeriod, Is.EqualTo(StoragePeriod.Month));
            Assert.That(definition.IndexedFields, Is.EqualTo(new[] { "MessageType" }));
            Assert.That(definition.Retention, Is.EqualTo(TimeSpan.FromDays(90)));
        }

        [Test]
        public void Configure_WithDuplicateField_DoesNotDuplicateMetadata()
        {
            var definition = new FlatStorageDefinition<LogRecord>()
                .PartitionBy(x => x.DeviceId)
                .PartitionBy(x => x.DeviceId)
                .Index(x => x.MessageType)
                .Index(x => x.MessageType);

            Assert.That(definition.PartitionFields.Count, Is.EqualTo(1));
            Assert.That(definition.IndexedFields.Count, Is.EqualTo(1));
        }

        [Test]
        public void RetainFor_WithNonPositiveDuration_FailsFast()
        {
            var definition = new FlatStorageDefinition<LogRecord>();

            Assert.Throws<ArgumentOutOfRangeException>(() => definition.RetainFor(TimeSpan.Zero));
        }
    }
}
