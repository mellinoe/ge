using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Engine.ProjectSystem
{
    public class AssemblyLoadSystem : GameSystem
    {
        public EngineLoadContext LoadContext { get; private set; } = new EngineLoadContext();

        public void CreateNewLoadContext()
        {
            LoadContext = new EngineLoadContext();
        }

        public IEnumerable<Assembly> LoadFromProjectManifest(ProjectManifest manifest, string rootPath)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string assembly in manifest.ManagedAssemblies)
            {
                string path = Path.Combine(rootPath, assembly);
                if (File.Exists(path))
                {
                    assemblies.Add(CopyAndLoad(path));
                }
                else
                {
                    throw new InvalidOperationException("A managed assembly from the manifest could not be found: " + path);
                }
            }

            return assemblies;
        }

        public Assembly CopyAndLoad(string path)
        {
            string tempPath = Path.GetTempFileName();
            File.Copy(path, tempPath, true);
            return LoadContext.LoadFromAssemblyPath(tempPath);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}
