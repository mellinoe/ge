using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Engine
{
    public static class EditorUtility
    {
        public static void SafeCopyDirectory(string sourceDir, string destinationDir)
        {
            // Borrowed from stackoverflow
            // http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c-sharp

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var destPath = dirPath.Replace(sourceDir, destinationDir);
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destinationDir), true);
            }
        }

        public static void ForceMoveFile(string source, string destination)
        {
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(source, destination);
        }

        public static void ShowFileInExplorer(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = Path.GetFullPath(path); // Normalize separators and get full path.
                string args = $"/select, \"{path}\"";
                Process.Start("explorer", args);
            }
        }

        public static bool IsEditorObject(GameObject go)
        {
            return go.Name.StartsWith("__");
        }
    }
}
