using ImGuiNET;
using System;
using System.IO;

namespace Engine.GUI
{
    public class FilePicker
    {
        private string _currentDirectory;
        private bool _isVisible;

        public FilePicker(string startingDirectory)
        {
            CurrentDirectory = startingDirectory;
        }

        public string CurrentDirectory
        {
            get { return _currentDirectory; }
            set { _currentDirectory = value; }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        public bool ShowFilePickerDialog(string title, out string selectedFile)
        {
            selectedFile = null;
            bool result = false;

            if (_isVisible)
            {
                ImGui.OpenPopup(title);
            }

            if (ImGui.BeginPopupModal(title))
            {
                if (!_isVisible)
                {
                    ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                    return false;
                }
                DirectoryInfo di;
                try
                {
                    di = new DirectoryInfo(_currentDirectory);
                }
                catch
                {
                    selectedFile = null;
                    ImGui.EndPopup();
                    return false;
                }

                ImGui.LabelText("Directory", di.FullName);

                if (di.Parent != null && ImGui.Button(".."))
                {
                    _currentDirectory = di.Parent.FullName;
                }

                FileSystemInfo[] children;
                try
                {
                    children = di.GetFileSystemInfos();
                }
                catch (UnauthorizedAccessException)
                {
                    children = Array.Empty<FileSystemInfo>();
                }
                foreach (var fsi in children)
                {
                    if (ImGui.Button(fsi.Name))
                    {
                        if (fsi is DirectoryInfo)
                        {
                            _currentDirectory = fsi.FullName;
                        }
                        else if (fsi is FileInfo)
                        {
                            selectedFile = fsi.FullName;
                            result = true;
                        }
                        else
                        {
                            throw new NotImplementedException($"Handling {fsi.GetType().Name} files is not supported.");
                        }
                    }
                }

                ImGui.EndPopup();
            }

            return result;
        }
    }
}
