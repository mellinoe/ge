using System;
using System.CommandLine;

namespace Engine
{
    public class EngineLaunchOptions
    {
        private bool _preferOpenGL = false;
        private AudioEnginePreference? _audioPreference;

        public bool PreferOpenGL => _preferOpenGL;
        public AudioEnginePreference? AudioPreference => _audioPreference;

        public EngineLaunchOptions(string[] args)
        {
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = "Editor";
                syntax.DefineOption("opengl", ref _preferOpenGL, "Prefer using the OpenGL rendering backend.");
                syntax.DefineOption(
                    "audio",
                    ref _audioPreference,
                    s =>
                    {
                        AudioEnginePreference pref;
                        if (!Enum.TryParse(s, true, out pref))
                        {
                            pref = AudioEnginePreference.Default;
                        }

                        return pref;
                    },
                    "Prefer using the OpenGL rendering backend.");
            });
        }

        public enum AudioEnginePreference
        {
            Default,
            OpenAL,
            None
        }
    }
}
