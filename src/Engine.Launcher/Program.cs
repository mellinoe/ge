using Engine.Assets;
using Engine.Audio;
using Engine.Behaviors;
using Engine.Editor;
using Engine.Graphics;
using Engine.Physics;
using Engine.ProjectSystem;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Veldrid.Assets;
using Veldrid.Platform;

namespace Engine
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            EngineLaunchOptions launchOptions = new EngineLaunchOptions(args);

            ProjectManifest projectManifest;
            string currentDir = AppContext.BaseDirectory;
            string manifestName = null;
            foreach (var file in Directory.EnumerateFiles(currentDir))
            {
                if (file.EndsWith("manifest"))
                {
                    if (manifestName != null)
                    {
                        Console.WriteLine("Error: Multiple project manifests in this directory: " + currentDir);
                        return -1;
                    }

                    manifestName = file;
                }
            }

            using (var fs = File.OpenRead(manifestName))
            using (var sr = new StreamReader(fs))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                try
                {
                    projectManifest = js.Deserialize<ProjectManifest>(jtr);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error was encountered while loading the project manifest.");
                    Console.WriteLine(e);
                    return -1;
                }
            }

            Game game = new Game();

            AssemblyLoadSystem als = new AssemblyLoadSystem();
            als.LoadFromProjectManifest(projectManifest, AppContext.BaseDirectory);
            game.SystemRegistry.Register(als);

            GraphicsPreferencesProvider graphicsProvider;
            string graphicsProviderName = projectManifest.GraphicsPreferencesProviderTypeName;
            if (graphicsProviderName != null)
            {
                graphicsProvider = GetProvider(als, graphicsProviderName);
            }
            else
            {
                graphicsProvider = new DefaultGraphicsPreferencesProvider();
            }

            var desiredInitialState = GraphicsPreferencesUtil.MapPreferencesState(graphicsProvider.WindowStatePreference);
            OpenTKWindow window = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (OpenTKWindow)new DedicatedThreadWindow(960, 540, desiredInitialState)
                : new SameThreadWindow(960, 540, desiredInitialState);
            window.Title = "ge.Main";
            GraphicsSystem gs = new GraphicsSystem(window, graphicsProvider);
            game.SystemRegistry.Register(gs);
            window.Closed += game.Exit;

            game.LimitFrameRate = false;

            InputSystem inputSystem = new InputSystem(window);
            inputSystem.RegisterCallback((input) =>
            {
                if (input.GetKeyDown(Key.F4) && (input.GetKey(Key.AltLeft) || input.GetKey(Key.AltRight)))
                {
                    game.Exit();
                }
                if (input.GetKeyDown(Key.F11))
                {
                    window.WindowState = window.WindowState == WindowState.Normal ? WindowState.BorderlessFullScreen : WindowState.Normal;
                }
            });
            game.SystemRegistry.Register(inputSystem);

            SceneLoaderSystem sls = new SceneLoaderSystem(game, game.SystemRegistry.GetSystem<GameObjectQuerySystem>());
            game.SystemRegistry.Register(sls);
            sls.AfterSceneLoaded += () => game.ResetDeltaTime();

            EngineLaunchOptions.AudioEnginePreference? audioPreference = launchOptions.AudioPreference;
            AudioEngineOptions audioEngineOptions =
                !audioPreference.HasValue ? AudioEngineOptions.Default
                : audioPreference == EngineLaunchOptions.AudioEnginePreference.None ? AudioEngineOptions.UseNullAudio
                : AudioEngineOptions.UseOpenAL;
            AudioSystem audioSystem = new AudioSystem(audioEngineOptions);
            game.SystemRegistry.Register(audioSystem);

            ImGuiRenderer imGuiRenderer = new ImGuiRenderer(gs.Context, window.NativeWindow, inputSystem);
            gs.SetImGuiRenderer(imGuiRenderer);

            AssetSystem assetSystem = new AssetSystem(Path.Combine(AppContext.BaseDirectory, projectManifest.AssetRoot), als.Binder);
            game.SystemRegistry.Register(assetSystem);

            BehaviorUpdateSystem bus = new BehaviorUpdateSystem(game.SystemRegistry);
            game.SystemRegistry.Register(bus);
            bus.Register(imGuiRenderer);

            PhysicsSystem ps = new PhysicsSystem(projectManifest.PhysicsLayers);
            game.SystemRegistry.Register(ps);

#if DEBUG
            ConsoleCommandSystem ccs = new ConsoleCommandSystem(game.SystemRegistry);
            game.SystemRegistry.Register(ccs);
#endif

            game.SystemRegistry.Register(new SynchronizationHelperSystem());

            SceneAsset scene;
            AssetID mainSceneID = projectManifest.OpeningScene.ID;
            if (mainSceneID.IsEmpty)
            {
                var scenes = assetSystem.Database.GetAssetsOfType(typeof(SceneAsset));
                if (!scenes.Any())
                {
                    Console.WriteLine("No scenes were available to load.");
                    return -1;
                }
                else
                {
                    mainSceneID = scenes.First();
                }
            }

            scene = assetSystem.Database.LoadAsset<SceneAsset>(mainSceneID);
            scene.GenerateGameObjects();

            RunStartupFunctions(projectManifest, als, game);

            game.RunMainLoop();

            return 0;
        }

        private static GraphicsPreferencesProvider GetProvider(AssemblyLoadSystem als, string graphicsProviderName)
        {
            Type t;
            if (!TryGetType(als, graphicsProviderName, out t))
            {
                throw new InvalidOperationException("Couldn't load the graphics provider specified in project manifest: " + graphicsProviderName);
            }

            PropertyInfo instanceGetter = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (instanceGetter == null)
            {
                throw new InvalidOperationException("Invalid graphics provider. Must have a public static Instance method.");
            }

            return (GraphicsPreferencesProvider)instanceGetter.GetGetMethod().Invoke(null, null);
        }

        private static void RunStartupFunctions(ProjectManifest projectManifest, AssemblyLoadSystem als, Game game)
        {
            foreach (var function in projectManifest.GameStartupFunctions)
            {
                Type t;
                if (!als.TryGetType(function.TypeName, out t))
                {
                    t = Type.GetType(function.TypeName);
                    if (t == null)
                    {
                        throw new InvalidOperationException("Invalid type name listed in project manifest's startup functions: " + function.TypeName);
                    }
                }

                MethodInfo mi = t.GetMethod(function.MethodName);
                if (mi == null)
                {
                    throw new InvalidOperationException("Invalid method name listed in startup function for type " + function.TypeName + ". Function name = " + function.MethodName);
                }

                var parameters = mi.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Game))
                {
                    throw new InvalidOperationException("Startup function must be a static method accepting one parameter of type Engine.Game");
                }

                mi.Invoke(null, new[] { game });
            }
        }

        public static bool TryGetType(AssemblyLoadSystem als, string typeName, out Type type)
        {
            if (!als.TryGetType(typeName, out type))
            {
                type = Type.GetType(typeName);
            }

            return type != null;
        }
    }
}
