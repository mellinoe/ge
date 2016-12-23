using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Engine.ProjectSystem
{
    public class AssemblyLoadSystem : GameSystem
    {
        private Assembly[] _loadedAssemblies;
        private EngineLoadContext _loadContext = new EngineLoadContext();
        public EngineSerializationBinder Binder { get; } = new EngineSerializationBinder();

        public void CreateNewLoadContext()
        {
            _loadContext = new EngineLoadContext();
            Binder.ClearAssemblies();
            _loadedAssemblies = Array.Empty<Assembly>();
        }

        public IEnumerable<Assembly> LoadFromProjectManifest(ProjectManifest manifest, string rootPath)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string assemblyPath in manifest.ManagedAssemblies)
            {
                string path = Path.Combine(rootPath, assemblyPath);
                if (File.Exists(path))
                {
                    Assembly assembly = CopyAndLoad(path);
                    assemblies.Add(assembly);
                    Binder.AddProjectAssembly(assembly);
                }
                else
                {
                    throw new InvalidOperationException("A managed assembly from the manifest could not be found: " + path);
                }
            }

            _loadedAssemblies = assemblies.ToArray();
            return assemblies;
        }

        private Assembly CopyAndLoad(string path)
        {
            string tempPath = Path.GetTempFileName();
            File.Copy(path, tempPath, true);
            return _loadContext.LoadFromAssemblyPath(tempPath);
        }

        public bool TryGetType(string typeName, out Type t)
        {
            foreach (var assm in _loadedAssemblies)
            {
                t = assm.GetType(typeName);
                if (t != null)
                {
                    return true;
                }
            }

            t = null;
            return false;
        }

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}
