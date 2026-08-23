using LagoVista;
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
        public void LagoVistaKey_SerializesAsStringAndRoundTrips()
        {
            var expected = LagoVistaKey.Parse("alpha-key");
            var source = new LagoVistaKeyDocument { Key = expected };

            var bson = source.ToBsonDocument();
            Assert.That(bson[nameof(LagoVistaKeyDocument.Key)].BsonType, Is.EqualTo(BsonType.String));
            Assert.That(bson[nameof(LagoVistaKeyDocument.Key)].AsString, Is.EqualTo(expected.Value));

            var roundTrip = BsonSerializer.Deserialize<LagoVistaKeyDocument>(bson);
            Assert.That(roundTrip.Key, Is.EqualTo(expected));
        }

        [Test]
        public void LagoVistaKey_EmptyValueSerializesAsNullAndRoundTrips()
        {
            var source = new LagoVistaKeyDocument();

            var bson = source.ToBsonDocument();
            Assert.That(bson[nameof(LagoVistaKeyDocument.Key)].BsonType, Is.EqualTo(BsonType.Null));

            var roundTrip = BsonSerializer.Deserialize<LagoVistaKeyDocument>(bson);
            Assert.That(String.IsNullOrWhiteSpace(roundTrip.Key.Value), Is.True);
        }

        [Test]
        public void NormalizedId32_SerializesAsStringAndRoundTrips()
        {
            var expected = NormalizedId32.Parse("F47AC10B58CC4372A5670E02B2C3D479");
            var source = new NormalizedIdDocument { Id = expected };

            var bson = source.ToBsonDocument();
            Assert.That(bson["_id"].BsonType, Is.EqualTo(BsonType.String));
            Assert.That(bson["_id"].AsString, Is.EqualTo(expected.Value));

            var roundTrip = BsonSerializer.Deserialize<NormalizedIdDocument>(bson);
            Assert.That(roundTrip.Id, Is.EqualTo(expected));
        }

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
        public void UtcTimestamp_EmptyValueSerializesAsNullAndRoundTrips()
        {
            var source = new UtcTimestampDocument();

            var bson = source.ToBsonDocument();
            Assert.That(bson[nameof(UtcTimestampDocument.Timestamp)].BsonType, Is.EqualTo(BsonType.Null));

            var roundTrip = BsonSerializer.Deserialize<UtcTimestampDocument>(bson);
            Assert.That(roundTrip.Timestamp.IsEmpty, Is.True);
        }

        [Test]
        public void UtcTimestamp_DeserializesLegacyBsonDateTime()
        {
            var instant = new DateTime(2026, 8, 23, 18, 30, 15, DateTimeKind.Utc);
            var bson = new BsonDocument(nameof(UtcTimestampDocument.Timestamp), new BsonDateTime(instant));

            var result = BsonSerializer.Deserialize<UtcTimestampDocument>(bson);
            Assert.That(result.Timestamp, Is.EqualTo(UtcTimestamp.FromDateTime(instant)));
        }

        private sealed class LagoVistaKeyDocument
        {
            public LagoVistaKey Key { get; set; }
        }

        private sealed class NormalizedIdDocument
        {
            public NormalizedId32 Id { get; set; }
        }

        private sealed class UtcTimestampDocument
        {
            public UtcTimestamp Timestamp { get; set; }
        }
    }
}
