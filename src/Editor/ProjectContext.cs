using Engine.ProjectSystem;
using System;
using System.IO;

namespace Engine.Editor
{
    public class ProjectContext
    {
        /// <summary>The path to the root directory of the project.</summary>
        public string ProjectRootPath { get; set; }
        /// <summary>The loaded project manifest.</summary>
        public ProjectManifest ProjectManifest { get; set; }
        public string ProjectManifestPath { get; set; }

        /// <summary>
        /// Constructs a new ProjectContext.
        /// </summary>
        /// <param name="projectRootPath">The root path of the project.</param>
        /// <param name="projectManifest">The loaded project manifest.</param>
        public ProjectContext(string projectRootPath, ProjectManifest projectManifest, string manifestPath)
        {
            if (string.IsNullOrEmpty(projectRootPath))
            {
                throw new ArgumentException("Parameter must not be empty or null.", nameof(projectRootPath));
            }
            if (projectManifest == null)
            {
                throw new ArgumentNullException(nameof(projectManifest));
            }

            ProjectRootPath = projectRootPath;
            ProjectManifest = projectManifest;
            ProjectManifestPath = manifestPath;
        }

        /// <summary>
        /// Constructs an absolute path from a project-relative path.
        /// </summary>
        /// <param name="projectRelativePath">A path relative to the project root.</param>
        /// <returns>An absolute path.</returns>
        public string GetPath(string projectRelativePath)
        {
            return Path.Combine(ProjectRootPath, projectRelativePath);
        }
    }
}