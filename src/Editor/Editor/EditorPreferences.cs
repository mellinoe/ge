using System.IO;

namespace Engine.Editor
{
    public class EditorPreferences : Preferences<EditorPreferences, EditorPreferences.Info>
    {
        public string LastOpenedScene { get; set; }

        public class Info : PreferencesInfo
        {
            public string StoragePath => Path.Combine("Ge", "EditorPreferences.json");
        }
    }
}
