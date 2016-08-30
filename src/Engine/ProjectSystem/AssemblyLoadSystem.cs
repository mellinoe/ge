using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Engine.ProjectSystem
{
    public class AssemblyLoadSystem : GameSystem
    {
        private EngineLoadContext _loadContext = new EngineLoadContext();
        public EngineSerializationBinder Binder { get; } = new EngineSerializationBinder();

        public void CreateNewLoadContext()
        {
            _loadContext = new EngineLoadContext();
            Binder.ClearAssemblies();
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

            return assemblies;
        }

        private Assembly CopyAndLoad(string path)
        {
            string tempPath = Path.GetTempFileName();
            File.Copy(path, tempPath, true);
            return _loadContext.LoadFromAssemblyPath(tempPath);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}
