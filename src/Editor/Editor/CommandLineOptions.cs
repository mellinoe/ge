using System.CommandLine;

namespace Engine.Editor
{
    public class CommandLineOptions
    {
        private bool _preferOpenGL = EditorPreferences.Instance.PreferOpenGL;
        private string _project;
        private string _scene;

        public bool PreferOpenGL => _preferOpenGL;
        public string Project => _project;
        public string Scene => _scene;

        public CommandLineOptions(string[] args)
        {
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = "Editor";
                syntax.DefineOption("opengl", ref _preferOpenGL, "Prefer using the OpenGL rendering backend.");
                syntax.DefineOption("project", ref _project, "Specifies the project to open.");
                syntax.DefineOption("scene", ref _scene, "Specifies the scene to open.");
            });
        }

        public CommandLineOptions()
        {
        }
    }
}
