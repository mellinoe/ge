using System;
using System.CommandLine;

namespace Engine.Editor
{
    public class CommandLineOptions
    {
        private bool _preferOpenGL = EditorPreferences.Instance.PreferOpenGL;
        private string _project;
        private string _scene;
        private AudioEnginePreference? _audioPreference;

        public bool PreferOpenGL => _preferOpenGL;
        public string Project => _project;
        public string Scene => _scene;
        public AudioEnginePreference? AudioPreference => _audioPreference;

        public CommandLineOptions(string[] args)
        {
            
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = "Editor";
                syntax.DefineOption("opengl", ref _preferOpenGL, "Prefer using the OpenGL rendering backend.");
                syntax.DefineOption("project|p", ref _project, "Specifies the project to open.");
                syntax.DefineOption("scene|s", ref _scene, "Specifies the scene to open.");
                syntax.DefineOption(
                    "audio",
                    ref _audioPreference,
                    s => (AudioEnginePreference)Enum.Parse(typeof(AudioEnginePreference), s, true),
                    "Specifies the audio engine to use.");
            });
        }

        public CommandLineOptions()
        {
        }

        public enum AudioEnginePreference
        {
            OpenAL,
            None
        }
    }
}
