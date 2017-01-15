using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Engine.Editor
{
    public abstract class Preferences<T, TInfo> where T : new() where TInfo : PreferencesInfo, new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadPreferences();
                }

                return _instance;
            }
        }

        public static string GetAppDataFolder()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetEnvironmentVariable("APPDATA");
            }
            else
            {
                return Environment.GetEnvironmentVariable("HOME");
            }
        }

        private static T LoadPreferences()
        {
            PreferencesInfo info = new TInfo();
            string preferencesFile = Path.Combine(GetAppDataFolder(), info.StoragePath);
            System.Diagnostics.Debug.WriteLine("Loading preferences from " + preferencesFile);
            if (File.Exists(preferencesFile))
            {
                string existingJson = File.ReadAllText(preferencesFile);
                try
                {
                    return JsonConvert.DeserializeObject<T>(existingJson);
                }
                catch (JsonSerializationException)
                {
                }
            }

            var ret = new T();
            string json = JsonConvert.SerializeObject(ret);
            string directory = Path.GetDirectoryName(preferencesFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(preferencesFile, json);
            return ret;
        }

        internal void Save()
        {
            PreferencesInfo info = new TInfo();
            string preferencesFile = Path.Combine(GetAppDataFolder(), info.StoragePath);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            string directory = Path.GetDirectoryName(preferencesFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(preferencesFile, json);
        }
    }

    public interface PreferencesInfo
    {
        string StoragePath { get; }
    }
}