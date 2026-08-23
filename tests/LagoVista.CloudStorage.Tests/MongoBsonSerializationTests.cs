using LagoVista.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System;

namespace LagoVista.CloudStorage.Tests
{
    public class MongoBsonSerializationTests
    {
        [Test]
        public void UtcTimestamp_SerializesAsCanonicalStringAndRoundTrips()
        {
            var expected = UtcTimestamp.FromDateTime(new DateTime(2026, 8, 23, 18, 30, 15, 123, DateTimeKind.Utc));
            var source = new UtcTimestampDocument { Timestamp = expected };

            var bson = source.ToBsonDocument();
            Assert.That(bson[nameof(UtcTimestampDocument.Timestamp)].BsonType, Is.EqualTo(BsonType.String));
            Assert.That(bson[nameof(UtcTimestampDocument.Timestamp)].AsString, Is.EqualTo("2026-08-23T18:30:15.123Z"));

            var roundTrip = BsonSerializer.Deserialize<UtcTimestampDocument>(bson);
            Assert.That(roundTrip.Timestamp, Is.EqualTo(expected));
        }

        [Test]
        public void UtcTimestamp_DeserializesLegacyBsonDateTime()
        {
            var instant = new DateTime(2026, 8, 23, 18, 30, 15, DateTimeKind.Utc);
            var bson = new BsonDocument(nameof(UtcTimestampDocument.Timestamp), new BsonDateTime(instant));

            var result = BsonSerializer.Deserialize<UtcTimestampDocument>(bson);
            Assert.That(result.Timestamp, Is.EqualTo(UtcTimestamp.FromDateTime(instant)));
        }

        private sealed class UtcTimestampDocument
        {
            public UtcTimestamp Timestamp { get; set; }
        }
    }
}
