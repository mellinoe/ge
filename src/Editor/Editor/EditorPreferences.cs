using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.Editor
{
    public class EditorPreferences : Preferences<EditorPreferences, EditorPreferences.Info>
    {
        private float _renderQuality = 1f;

        public Dictionary<string, List<string>> OpenedSceneHistory { get; set; } = new Dictionary<string, List<string>>();

        public string LastOpenedProjectRoot { get; set; }

        public bool PreferOpenGL { get; set; }

        public float RenderQuality
        {
            get { return _renderQuality; }
            set { _renderQuality = MathUtil.Clamp(value, 0.1f, 1); }
        }

        public string GetLastOpenedScene(string project)
        {
            var list = GetProjectSceneHistory(project);
            return list.LastOrDefault();
        }

        public List<string> GetProjectSceneHistory(string project)
        {
            List<string> list;
            if (!OpenedSceneHistory.TryGetValue(project, out list))
            {
                list = new List<string>();
                OpenedSceneHistory.Add(project, list);
            }

            return list;
        }

        private const int OpenedSceneHistoryLimit = 10;

        public void SetLatestScene(string project, string path)
        {
            var list = GetProjectSceneHistory(project);
            list.Remove(path);
            list.Add(path);

            if (list.Count > OpenedSceneHistoryLimit)
            {
                list.RemoveAt(0);
            }
        }

        public class Info : PreferencesInfo
        {
            public string StoragePath => Path.Combine("Ge", "EditorPreferences.json");
        }
    }
}
