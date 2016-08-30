using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Engine.ProjectSystem
{
    public class EngineSerializationBinder : SerializationBinder
    {
        private readonly Dictionary<string, Assembly> _projectLoadedAssemblies = new Dictionary<string, Assembly>();
        private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder();

        public void ClearAssemblies() => _projectLoadedAssemblies.Clear();

        public void AddProjectAssembly(Assembly assembly)
        {
            _projectLoadedAssemblies.Add(assembly.GetName().Name, assembly);
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            _defaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            Assembly assembly;
            if (_projectLoadedAssemblies.TryGetValue(assemblyName, out assembly))
            {
                return assembly.GetType(typeName);
            }
            else
            {
                return _defaultBinder.BindToType(assemblyName, typeName);
            }
        }
    }
}
