using System;
using System.IO;
using System.Linq;

namespace Engine.Editor
{
    public class ProjectPublisher
    {
        private readonly string _publishItemsRoot;
        public string[] PublishTargets { get; set; }

        public ProjectPublisher()
        {
            _publishItemsRoot = Path.Combine(AppContext.BaseDirectory, "PublishItems");
            PublishTargets = Directory.EnumerateDirectories(_publishItemsRoot)
                .Select(d => new DirectoryInfo(d).Name).ToArray();
        }

        public void PublishProject(ProjectContext projectContext, string target, string outputDir)
        {
            if (!PublishTargets.Contains(target))
            {
                throw new InvalidOperationException("No publish target with name " + target);
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string sourcePath = Path.Combine(_publishItemsRoot, target);
            EditorUtility.SafeCopyDirectory(sourcePath, outputDir);
            string assetPath = Path.Combine(projectContext.ProjectRootPath, projectContext.ProjectManifest.AssetRoot);
            EditorUtility.SafeCopyDirectory(assetPath, assetPath.Replace(projectContext.ProjectRootPath, outputDir));

            string manifestFile = Directory.GetFiles(projectContext.ProjectRootPath, "*.manifest").SingleOrDefault();
            if (manifestFile == null)
            {
                throw new InvalidOperationException("Couldn't locate the project manifest to publish.");
            }
            File.Copy(manifestFile, manifestFile.Replace(projectContext.ProjectRootPath, outputDir), overwrite: true);
        }
    }
}
