using System.Collections.Generic;
using Veldrid.Assets;

namespace Engine.ProjectSystem
{
    public class ProjectManifest
    {
        public string Name { get; set; }
        public string AssetRoot { get; set; } = "Assets";
        public List<string> ManagedAssemblies { get; set; } = new List<string>();
        public AssetID OpeningScene { get; set; }

        public ProjectManifest()
        {
        }
    }
}
