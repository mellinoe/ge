using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Engine.Editor
{
    public class ProjectPublisher
    {
        private readonly string _publishItemsRoot;
        private const string LauncherName = "Engine.Launcher";
        private static string GetExeSuffix(string target)
        {
            return target.ToLowerInvariant().Contains("win") ? ".exe" : string.Empty;
        }

        public string[] PublishTargets { get; set; }

        public ProjectPublisher()
        {
            _publishItemsRoot = Path.Combine(AppContext.BaseDirectory, "PublishItems");
            if (Directory.Exists(_publishItemsRoot))
            {
                PublishTargets = Directory.EnumerateDirectories(_publishItemsRoot)
                    .Select(d => new DirectoryInfo(d).Name).ToArray();
            }
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

            string launcherExePath = Path.Combine(outputDir, LauncherName + GetExeSuffix(target));
            string launcherDllPath = Path.Combine(outputDir, LauncherName + ".dll");

            string projectNamedLauncher = launcherExePath.Replace(LauncherName, projectContext.ProjectManifest.Name);
            EditorUtility.ForceMoveFile(launcherExePath, projectNamedLauncher);
            EditorUtility.ForceMoveFile(launcherDllPath, launcherDllPath.Replace(LauncherName, projectContext.ProjectManifest.Name));

#if !DEBUG
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && target.ToLowerInvariant().Contains("windows"))
            {
                TryChangeSubsystemLink(projectNamedLauncher, "windows");
            }
#endif

            EditorUtility.ShowFileInExplorer(projectNamedLauncher);
        }

        private void TryChangeSubsystemLink(string launcherExePath, string subsystem)
        {
            string vsInstallDir = GetLatestVSInstallDir();
            if (vsInstallDir != null)
            {
                string editbinPath = Path.Combine(vsInstallDir, "VC", "bin", "editbin.exe");
                if (File.Exists(editbinPath))
                {
                    string args = $"/subsystem:{subsystem} {launcherExePath}";
                    Process.Start(editbinPath, args);
                }
            }
        }

        private string GetLatestVSInstallDir()
        {
            string path = GetVSInstallDir("14.0") ?? GetVSInstallDir("15.0");
            return path;
        }

        private string GetVSInstallDir(string version)
        {
            string installationPath = null;
            installationPath = (string)Registry.GetValue(
               $"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio\\{version}\\",
                "InstallDir",
                null);
            var di = new DirectoryInfo(installationPath);
            return di.Parent.Parent.FullName;
        }
    }
}
