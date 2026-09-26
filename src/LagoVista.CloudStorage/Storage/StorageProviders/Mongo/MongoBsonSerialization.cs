using LagoVista;
using LagoVista.Core;
using LagoVista.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
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

                BsonSerializer.TryRegisterSerializer(
                    typeof(object),
                    new ObjectSerializer(type =>
                        ObjectSerializer.DefaultAllowedTypes(type) ||
                        typeof(JToken).IsAssignableFrom(type)));

                BsonSerializer.RegisterDiscriminatorConvention(
                    typeof(EntityHeader),
                    new EntityHeaderLegacyDiscriminatorConvention());

                if (!BsonClassMap.IsClassMapRegistered(typeof(EntityHeader)))
                {
                    BsonClassMap.RegisterClassMap<EntityHeader>(classMap =>
                    {
                        classMap.AutoMap();
                        classMap.SetIdMember(null);
                        classMap.GetMemberMap(nameof(EntityHeader.Id)).SetElementName(nameof(EntityHeader.Id));
                        classMap.SetIgnoreExtraElements(true);
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
                BsonSerializer.RegisterSerializer(typeof(LagoVistaIcon), new LagoVistaIconBsonSerializer());
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
            if (!visiting.Add(type)) return;

            try
            {
                if (type.IsArray)
                {
                    PrepareTypeGraph(type.GetElementType(), visiting);
                    return;
                }

                // Walk generic arguments even for System collection/container types. This is
                // important for shapes such as List<DeviceMessageDefinitionField> and
                // List<KeyValuePair<string, object>> where the interesting LagoVista type
                // is nested inside a framework container.
                if (type.IsGenericType)
                {
                    foreach (var argument in type.GetGenericArguments())
                        PrepareTypeGraph(argument, visiting);
                }

                if (ShouldSkipType(type))
                    return;

                // Mongo's built-in class mapper handles virtual overrides correctly. We only
                // need the compatibility serializer for true CLR member hiding ("new").
                if (!IsEntityHeaderType(type) &&
                    HasShadowedSerializableProperty(type) &&
                    _preparedTypes.TryAdd(type, 0))
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

        private static bool IsEntityHeaderType(Type type)
        {
            return type == typeof(EntityHeader) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntityHeader<>));
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
            var properties = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializableProperty(property)) continue;

                    var baseDefinition = property.GetMethod.GetBaseDefinition();
                    if (!properties.TryGetValue(property.Name, out var existingBaseDefinition))
                    {
                        properties[property.Name] = baseDefinition;
                        continue;
                    }

                    // Same virtual slot means this is an override, not a hidden member.
                    if (existingBaseDefinition == baseDefinition)
                        continue;

                    return true;
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

            // EntityBase.Id is intentionally ignored by Newtonsoft because Cosmos persists
            // StoredId as "id". Mongo's native convention, however, persists the CLR Id
            // member as "_id". ShadowedMemberBsonSerializer must preserve that native
            // Mongo identity contract even though the property carries JsonIgnore.
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null &&
                !(property.Name == nameof(EntityBase.Id) && property.DeclaringType == typeof(EntityBase)))
                return false;

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

    internal sealed class EntityHeaderLegacyDiscriminatorConvention : IDiscriminatorConvention
    {
        public string ElementName => "_t";

        public Type GetActualType(MongoDB.Bson.IO.IBsonReader bsonReader, Type nominalType)
        {
            if (bsonReader == null) throw new ArgumentNullException(nameof(bsonReader));
            if (nominalType == null) throw new ArgumentNullException(nameof(nominalType));

            var bookmark = bsonReader.GetBookmark();
            try
            {
                bsonReader.ReadStartDocument();

                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var elementName = bsonReader.ReadName(MongoDB.Bson.IO.Utf8NameDecoder.Instance);
                    if (!String.Equals(elementName, ElementName, StringComparison.Ordinal))
                    {
                        bsonReader.SkipValue();
                        continue;
                    }

                    if (bsonReader.GetCurrentBsonType() != BsonType.String)
                    {
                        bsonReader.SkipValue();
                        return nominalType;
                    }

                    var discriminator = bsonReader.ReadString();
                    if (String.Equals(discriminator, "ieh", StringComparison.OrdinalIgnoreCase) &&
                        nominalType.IsAssignableFrom(typeof(ImageEntityHeader)))
                        return typeof(ImageEntityHeader);

                    if (String.Equals(discriminator, "chref", StringComparison.OrdinalIgnoreCase) &&
                        nominalType.IsAssignableFrom(typeof(ChildReference)))
                        return typeof(ChildReference);

                    // "_t" is a legacy EntityHeader wire marker, not a CLR type discriminator.
                    // In particular, EntityHeader<T> instances also persist "_t":"eh", so
                    // resolving "eh" globally to EntityHeader would break typed headers.
                    return nominalType;
                }

                return nominalType;
            }
            finally
            {
                bsonReader.ReturnToBookmark(bookmark);
            }
        }

        public BsonValue GetDiscriminator(Type nominalType, Type actualType)
        {
            if (actualType == typeof(ImageEntityHeader))
                return new BsonString("ieh");

            if (actualType == typeof(ChildReference))
                return new BsonString("chref");

            return new BsonString("eh");
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
                var elementName = context.Reader.ReadName(MongoDB.Bson.IO.Utf8NameDecoder.Instance);

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

    internal sealed class LagoVistaIconBsonSerializer : SerializerBase<LagoVistaIcon>
    {
        public override LagoVistaIcon Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var reader = context.Reader;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return new LagoVistaIcon("icon-fo-gears-2");

                case BsonType.String:
                    return new LagoVistaIcon(reader.ReadString());

                case BsonType.Document:
                    var document = BsonDocumentSerializer.Instance.Deserialize(context);
                    var value = document.GetValue("Value", BsonNull.Value);
                    if (value.IsBsonNull)
                        value = document.GetValue("value", BsonNull.Value);

                    if (value.IsString && !String.IsNullOrWhiteSpace(value.AsString))
                        return new LagoVistaIcon(value.AsString);

                    return new LagoVistaIcon("icon-fo-gears-2");

                default:
                    throw new BsonSerializationException($"Cannot deserialize {nameof(LagoVistaIcon)} from BSON type {reader.GetCurrentBsonType()}.");
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LagoVistaIcon value)
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
