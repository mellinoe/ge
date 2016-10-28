using System;
using System.Collections.Concurrent;
using System.IO;
using Wire;
using Wire.SerializerFactories;
using Wire.ValueSerializers;
using System.Reflection;
using System.Linq;
using Wire.Extensions;
using Newtonsoft.Json;
using Wire.Internal;

namespace Engine.Assets.Wire
{
    public class WireComponentSerializerFactory : ValueSerializerFactory
    {
        private static readonly Type s_componentType = typeof(Component);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type, ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            ComponentSerializer cs = new ComponentSerializer(type);
            return cs;
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return s_componentType.IsAssignableFrom(type);
        }

        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return s_componentType.IsAssignableFrom(type);
        }
    }

    public class ComponentSerializer : ValueSerializer
    {
        private readonly ObjectSerializer _os;
        private readonly PropertyInfo[] _properties;
        private readonly Type _type;

        public ComponentSerializer(Type type)
        {
            _type = type;
            _os = new ObjectSerializer(type);

            _properties = type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .Where(IsValidProperty).ToArray();
        }

        public override Type GetElementType() => _type;

        public override object ReadValue([NotNull] Stream stream, [NotNull] DeserializerSession session)
        {
            object component = Activator.CreateInstance(_type);
            foreach (var prop in _properties)
            {
                object value = stream.ReadObject(session);
                prop.SetValue(component, value);
            }

            return component;
        }

        public override void WriteValue([NotNull] Stream stream, object value, [NotNull] SerializerSession session)
        {
            foreach (var prop in _properties)
            {
                stream.WriteObject(
                    prop.GetValue(value),
                    prop.PropertyType,
                    session.Serializer.GetSerializerByType(prop.PropertyType),
                    false,
                    session);
            }
        }

        public override void WriteManifest([NotNull] Stream stream, [NotNull] SerializerSession session)
        {
            _os.WriteManifest(stream, session);
        }

        private bool IsValidProperty(PropertyInfo pi)
        {
            return pi.GetCustomAttribute<JsonIgnoreAttribute>() == null
                && pi.SetMethod != null;
        }
    }
}
