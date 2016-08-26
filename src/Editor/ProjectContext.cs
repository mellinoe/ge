using Engine.ProjectSystem;

namespace Engine.Editor
{
    public class ProjectContext
    {
        public string ProjectRootPath { get; set; }
        public ProjectManifest ProjectManifest { get; set; }

        public ProjectContext(string projectRootPath, ProjectManifest projectManifest)
        {
            ProjectRootPath = projectRootPath;
            ProjectManifest = projectManifest;
        }
    }
}