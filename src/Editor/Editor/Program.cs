using Engine.Behaviors;
using Engine.Graphics;
using System.Runtime.InteropServices;
using Veldrid.Platform;
using Veldrid.Graphics;
using System.Numerics;
using System.IO;
using System;
using Engine.Physics;
using Engine.Editor;
using Engine.Assets;
using Engine;
using Veldrid.Assets;
using ImGuiNET;
using System.Reflection;
using Engine.ProjectSystem;
using Engine.Audio;

namespace Engine.Editor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            OpenTKWindow window = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? (OpenTKWindow)new DedicatedThreadWindow(960, 540, WindowState.Maximized) 
                : new SameThreadWindow(960, 540, WindowState.Maximized);
            window.Title = "ge.Editor";
            Game game = new Game();
            GraphicsSystem gs = new GraphicsSystem(window, EditorPreferences.Instance.PreferOpenGL);
            gs.Context.ResourceFactory.AddShaderLoader(new EmbeddedResourceShaderLoader(typeof(Program).GetTypeInfo().Assembly));
            game.SystemRegistry.Register(gs);
            game.LimitFrameRate = true;

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

            EditorSceneLoaderSystem esls = new EditorSceneLoaderSystem(game.SystemRegistry.GetSystem<GameObjectQuerySystem>());
            game.SystemRegistry.Register<SceneLoaderSystem>(esls);

            AudioSystem audioSystem = new AudioSystem();
            game.SystemRegistry.Register(audioSystem);

            BehaviorUpdateSystem bus = new BehaviorUpdateSystem(game.SystemRegistry);
            game.SystemRegistry.Register(bus);
            bus.Register(imGuiRenderer);

            PhysicsSystem ps = new PhysicsSystem();
            game.SystemRegistry.Register(ps);

            ConsoleCommandSystem ccs = new ConsoleCommandSystem(game.SystemRegistry);
            game.SystemRegistry.Register(ccs);

            window.Closed += game.Exit;

            var editorSystem = new EditorSystem(game.SystemRegistry);
            editorSystem.DiscoverComponentsFromAssembly(typeof(Program).GetTypeInfo().Assembly);
            // Editor system registers itself.

            // Force-load prefs.
            var prefs = EditorPreferences.Instance;

            game.RunMainLoop();

            EditorPreferences.Instance.Save();
        }
    }
}
