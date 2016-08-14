namespace Engine.ProjectSystem
{
    public class Project
    {
        public string Name { get; set; }
        public string AssetRoot { get; set; } = "Assets";

        public Project (string name )
        {
            Name = name;
        }
    }
}
