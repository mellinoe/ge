using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.Editor
{
    public class EditorPreferences : Preferences<EditorPreferences, EditorPreferences.Info>
    {
        public List<string> OpenedSceneHistory { get; set; } = new List<string>();

        public string LastOpenedScene => OpenedSceneHistory.LastOrDefault();

        private const int OpenedSceneHistoryLimit = 10;

        public void SetLatestScene(string path)
        {
            OpenedSceneHistory.Remove(path);
            OpenedSceneHistory.Add(path);

            if (OpenedSceneHistory.Count > OpenedSceneHistoryLimit)
            {
                OpenedSceneHistory.RemoveAt(0);
            }
        }

        public class Info : PreferencesInfo
        {
            public string StoragePath => Path.Combine("Ge", "EditorPreferences.json");
        }
    }
}
