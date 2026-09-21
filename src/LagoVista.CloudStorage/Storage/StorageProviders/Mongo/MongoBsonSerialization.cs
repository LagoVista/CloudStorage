using LagoVista;
using LagoVista.Core;
using LagoVista.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    internal static class MongoBsonSerialization
    {
        private static readonly object _syncRoot = new object();
        private static readonly ConcurrentDictionary<Type, byte> _preparedTypes = new ConcurrentDictionary<Type, byte>();
        private static bool _configured;

        public static void Configure()
        {
            if (_configured) return;

            lock (_syncRoot)
            {
                if (_configured) return;

                if (!BsonClassMap.IsClassMapRegistered(typeof(EntityHeader)))
                {
                    BsonClassMap.RegisterClassMap<EntityHeader>(classMap =>
                    {
                        classMap.AutoMap();
                        classMap.SetIdMember(null);
                        classMap.GetMemberMap(nameof(EntityHeader.Id)).SetElementName(nameof(EntityHeader.Id));
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(Label)))
                {
                    BsonClassMap.RegisterClassMap<Label>(classMap =>
                    {
                        classMap.AutoMap();
                        classMap.SetIgnoreExtraElements(true);
                    });
                }

                BsonSerializer.RegisterSerializer(typeof(LagoVistaKey), new LagoVistaKeyBsonSerializer());
                BsonSerializer.RegisterSerializer(typeof(NormalizedId32), new NormalizedId32BsonSerializer());
                BsonSerializer.RegisterSerializer(typeof(UtcTimestamp), new UtcTimestampBsonSerializer());
                BsonSerializer.TryRegisterSerializer(typeof(JObject), new JObjectBsonSerializer());
                _configured = true;
            }
        }

        public static void ConfigureForType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            Configure();
            PrepareTypeGraph(type, new HashSet<Type>());
        }

        private static void PrepareTypeGraph(Type type, HashSet<Type> visiting)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (ShouldSkipType(type)) return;
            if (!visiting.Add(type)) return;

            try
            {
                if (type.IsArray)
                {
                    PrepareTypeGraph(type.GetElementType(), visiting);
                    return;
                }

                if (type.IsGenericType)
                {
                    foreach (var argument in type.GetGenericArguments())
                        PrepareTypeGraph(argument, visiting);
                }

                if (HasShadowedSerializableProperty(type) && _preparedTypes.TryAdd(type, 0))
                {
                    var serializerType = typeof(ShadowedMemberBsonSerializer<>).MakeGenericType(type);
                    var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType);
                    BsonSerializer.TryRegisterSerializer(type, serializer);
                }

                foreach (var property in GetMostDerivedSerializableProperties(type))
                    PrepareTypeGraph(property.PropertyType, visiting);
            }
            finally
            {
                visiting.Remove(type);
            }
        }

        private static bool ShouldSkipType(Type type)
        {
            if (type == null) return true;
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
                type == typeof(Guid) || typeof(BsonValue).IsAssignableFrom(type) || typeof(JToken).IsAssignableFrom(type))
                return true;

            if (type.Namespace != null &&
                (type.Namespace.StartsWith("System", StringComparison.Ordinal) ||
                 type.Namespace.StartsWith("Microsoft", StringComparison.Ordinal) ||
                 type.Namespace.StartsWith("MongoDB", StringComparison.Ordinal)))
                return true;

            return false;
        }

        private static bool HasShadowedSerializableProperty(Type type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializableProperty(property)) continue;
                    if (!names.Add(property.Name)) return true;
                }
            }

            return false;
        }

        internal static IReadOnlyList<PropertyInfo> GetMostDerivedSerializableProperties(Type type)
        {
            var properties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializableProperty(property)) continue;
                    if (!properties.ContainsKey(property.Name))
                        properties[property.Name] = property;
                }
            }

            return properties.Values.OrderBy(property => property.MetadataToken).ToList();
        }

        private static bool IsSerializableProperty(PropertyInfo property)
        {
            if (property == null || property.GetIndexParameters().Length != 0) return false;
            if (property.GetMethod == null || !property.GetMethod.IsPublic) return false;
            if (property.SetMethod == null || !property.SetMethod.IsPublic) return false;
            if (property.GetCustomAttribute<BsonIgnoreAttribute>() != null) return false;
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null) return false;
            return true;
        }

        internal static string GetElementName(PropertyInfo property)
        {
            var element = property.GetCustomAttribute<BsonElementAttribute>();
            if (element != null && !String.IsNullOrWhiteSpace(element.ElementName))
                return element.ElementName;

            if (property.GetCustomAttribute<BsonIdAttribute>() != null ||
                String.Equals(property.Name, "Id", StringComparison.Ordinal))
                return "_id";

            return property.Name;
        }
    }

    internal sealed class ShadowedMemberBsonSerializer<T> : SerializerBase<T>, IBsonDocumentSerializer
    {
        private sealed class Member
        {
            public PropertyInfo Property { get; set; }
            public string ElementName { get; set; }
        }

        private readonly IReadOnlyList<Member> _members;
        private readonly IReadOnlyDictionary<string, Member> _membersByElement;
        private readonly IReadOnlyDictionary<string, Member> _membersByName;

        public ShadowedMemberBsonSerializer()
        {
            _members = MongoBsonSerialization.GetMostDerivedSerializableProperties(typeof(T))
                .Select(property => new Member
                {
                    Property = property,
                    ElementName = MongoBsonSerialization.GetElementName(property)
                })
                .ToList();

            _membersByElement = _members
                .GroupBy(member => member.ElementName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            _membersByName = _members
                .ToDictionary(member => member.Property.Name, StringComparer.Ordinal);
        }

        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Reader.GetCurrentBsonType() == BsonType.Null)
            {
                context.Reader.ReadNull();
                return default(T);
            }

            var instance = Activator.CreateInstance<T>();
            context.Reader.ReadStartDocument();

            while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var elementName = context.Reader.ReadName();

                if (!_membersByElement.TryGetValue(elementName, out var member))
                {
                    // Accept the CLR name as an alias for legacy JSON-shaped documents.
                    _membersByName.TryGetValue(elementName, out member);
                }

                if (member == null)
                {
                    context.Reader.SkipValue();
                    continue;
                }

                var serializer = BsonSerializer.LookupSerializer(member.Property.PropertyType);
                var value = serializer.Deserialize(
                    context,
                    new BsonDeserializationArgs { NominalType = member.Property.PropertyType });
                member.Property.SetValue(instance, value);
            }

            context.Reader.ReadEndDocument();
            return instance;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (ReferenceEquals(value, null))
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteStartDocument();

            foreach (var member in _members)
            {
                context.Writer.WriteName(member.ElementName);
                var serializer = BsonSerializer.LookupSerializer(member.Property.PropertyType);
                serializer.Serialize(
                    context,
                    new BsonSerializationArgs { NominalType = member.Property.PropertyType },
                    member.Property.GetValue(value));
            }

            context.Writer.WriteEndDocument();
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (_membersByName.TryGetValue(memberName, out var member))
            {
                var serializer = BsonSerializer.LookupSerializer(member.Property.PropertyType);
                serializationInfo = new BsonSerializationInfo(member.ElementName, serializer, member.Property.PropertyType);
                return true;
            }

            serializationInfo = null;
            return false;
        }
    }

    internal sealed class JObjectBsonSerializer : SerializerBase<JObject>
    {
        public override JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Reader.GetCurrentBsonType() == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }

            var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
            return JObject.Parse(document.ToJson());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            var document = BsonDocument.Parse(value.ToString(Formatting.None));
            BsonDocumentSerializer.Instance.Serialize(context, args, document);
        }
    }

    internal sealed class LagoVistaKeyBsonSerializer : SerializerBase<LagoVistaKey>
    {
        public override LagoVistaKey Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var reader = context.Reader;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return default(LagoVistaKey);

                case BsonType.String:
                    return LagoVistaKey.Parse(reader.ReadString());

                default:
                    throw new BsonSerializationException($"Cannot deserialize {nameof(LagoVistaKey)} from BSON type {reader.GetCurrentBsonType()}.");
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LagoVistaKey value)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (String.IsNullOrWhiteSpace(value.Value))
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteString(value.Value);
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
