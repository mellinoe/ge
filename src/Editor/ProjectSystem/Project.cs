namespace Engine.Editor.ProjectSystem
{
    public class ProjectManifest
    {
        public string Name { get; set; }
        public string AssetRoot { get; set; } = "Assets";

        public ProjectManifest(string name)
        {
            Name = name;
        }
    }
}
