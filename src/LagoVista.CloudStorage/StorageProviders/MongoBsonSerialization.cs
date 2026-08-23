using LagoVista;
using LagoVista.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace LagoVista.CloudStorage.StorageProviders
{
    internal static class MongoBsonSerialization
    {
        private static readonly object _syncRoot = new object();
        private static bool _configured;

        public static void Configure()
        {
            if (_configured) return;

            lock (_syncRoot)
            {
                if (_configured) return;
                BsonSerializer.RegisterSerializer(typeof(NormalizedId32), new NormalizedId32BsonSerializer());
                BsonSerializer.RegisterSerializer(typeof(UtcTimestamp), new UtcTimestampBsonSerializer());
                _configured = true;
            }
        }
    }

    internal sealed class NormalizedId32BsonSerializer : SerializerBase<NormalizedId32>
    {
        public override NormalizedId32 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return NormalizedId32.Parse(context.Reader.ReadString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, NormalizedId32 value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.Writer.WriteString(value.Value);
        }
    }

    internal sealed class UtcTimestampBsonSerializer : SerializerBase<UtcTimestamp>
    {
        public override UtcTimestamp Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var reader = context.Reader;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return default(UtcTimestamp);

                case BsonType.String:
                    return UtcTimestamp.Parse(reader.ReadString());

                case BsonType.DateTime:
                    return UtcTimestamp.FromDateTime(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(reader.ReadDateTime()));

                default:
                    throw new BsonSerializationException($"Cannot deserialize {nameof(UtcTimestamp)} from BSON type {reader.GetCurrentBsonType()}.");
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UtcTimestamp value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (value.IsEmpty)
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteString(value.ToString());
        }
    }
}
