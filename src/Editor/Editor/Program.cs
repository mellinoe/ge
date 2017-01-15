using Engine.Behaviors;
using Engine.Graphics;
using System.Runtime.InteropServices;
using Veldrid.Platform;
using Veldrid.Graphics;
using System.IO;
using System;
using Engine.Physics;
using Engine.Assets;
using System.Reflection;
using Engine.ProjectSystem;
using Engine.Audio;

namespace Engine.Editor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CommandLineOptions commandLineOptions = new CommandLineOptions(args);
            // Force-load prefs.
            var prefs = EditorPreferences.Instance;

            OpenTKWindow window = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (OpenTKWindow)new DedicatedThreadWindow(960, 540, WindowState.Maximized)
                : new SameThreadWindow(960, 540, WindowState.Maximized);
            window.Title = "ge.Editor";
            Game game = new Game();
            GraphicsBackEndPreference backEndPref = commandLineOptions.PreferOpenGL ? GraphicsBackEndPreference.OpenGL : GraphicsBackEndPreference.None;
            GraphicsSystem gs = new GraphicsSystem(window, prefs.RenderQuality, backEndPref);
            gs.Context.ResourceFactory.AddShaderLoader(new EmbeddedResourceShaderLoader(typeof(Program).GetTypeInfo().Assembly));
            game.SystemRegistry.Register(gs);
            game.LimitFrameRate = false;

            InputSystem inputSystem = new InputSystem(window);
            inputSystem.RegisterCallback((input) =>
            {
                if (input.GetKeyDown(Key.F4) && (input.GetKey(Key.AltLeft) || input.GetKey(Key.AltRight)))
                {
                    game.Exit();
                }
            });

            game.SystemRegistry.Register(inputSystem);

            ImGuiRenderer imGuiRenderer = new ImGuiRenderer(gs.Context, window.NativeWindow, inputSystem);
            gs.SetImGuiRenderer(imGuiRenderer);

            var als = new AssemblyLoadSystem();
            game.SystemRegistry.Register(als);

            AssetSystem assetSystem = new EditorAssetSystem(Path.Combine(AppContext.BaseDirectory, "Assets"), als.Binder);
            game.SystemRegistry.Register(assetSystem);

            EditorSceneLoaderSystem esls = new EditorSceneLoaderSystem(game, game.SystemRegistry.GetSystem<GameObjectQuerySystem>());
            game.SystemRegistry.Register<SceneLoaderSystem>(esls);
            esls.AfterSceneLoaded += () => game.ResetDeltaTime();

            CommandLineOptions.AudioEnginePreference? audioPreference = commandLineOptions.AudioPreference;
            AudioEngineOptions audioEngineOptions =
                !audioPreference.HasValue ? AudioEngineOptions.Default
                : audioPreference == CommandLineOptions.AudioEnginePreference.None ? AudioEngineOptions.UseNullAudio
                : AudioEngineOptions.UseOpenAL;
            AudioSystem audioSystem = new AudioSystem(audioEngineOptions);
            game.SystemRegistry.Register(audioSystem);

            BehaviorUpdateSystem bus = new BehaviorUpdateSystem(game.SystemRegistry);
            game.SystemRegistry.Register(bus);
            bus.Register(imGuiRenderer);

            PhysicsSystem ps = new PhysicsSystem(PhysicsLayersDescription.Default);
            game.SystemRegistry.Register(ps);

            ConsoleCommandSystem ccs = new ConsoleCommandSystem(game.SystemRegistry);
            game.SystemRegistry.Register(ccs);

            game.SystemRegistry.Register(new SynchronizationHelperSystem());

            window.Closed += game.Exit;

            var editorSystem = new EditorSystem(game.SystemRegistry, commandLineOptions, imGuiRenderer);
            editorSystem.DiscoverComponentsFromAssembly(typeof(Program).GetTypeInfo().Assembly);
            // Editor system registers itself.

            game.RunMainLoop();

            window.NativeWindow.Dispose();

            EditorPreferences.Instance.Save();
        }
    }
}
