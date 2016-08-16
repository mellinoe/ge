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
using System.Linq;

namespace Ge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            OpenTKWindow window = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (OpenTKWindow)new DedicatedThreadWindow() : new SameThreadWindow();
            window.Title = "ge.Main";
            window.Visible = true;
            Game game = new Game();
            GraphicsSystem gs = new GraphicsSystem(window);
            game.SystemRegistry.Register(gs);
            gs.AddFreeRenderItem(new ShadowMapPreview(gs.Context));

            InputSystem inputSystem = new InputSystem(window);
            inputSystem.RegisterCallback((input) =>
            {
                if (input.GetKeyDown(Key.F4) && (input.GetKey(Key.AltLeft) || input.GetKey(Key.AltRight)))
                {
                    game.Exit();
                }
                if (input.GetKeyDown(Key.F11))
                {
                    window.WindowState = window.WindowState == WindowState.Normal ? WindowState.FullScreen : WindowState.Normal;
                }
                if (input.GetKeyDown(Key.F12))
                {
                    gs.ToggleOctreeVisualizer();
                }
            });

            game.SystemRegistry.Register(inputSystem);

            ImGuiRenderer imGuiRenderer = new ImGuiRenderer(gs.Context, window.NativeWindow, inputSystem);
            gs.AddFreeRenderItem(imGuiRenderer);
            ImGuiNET.ImGui.GetIO().FontAllowUserScaling = true;

            AssetSystem assetSystem = new AssetSystem();
            game.SystemRegistry.Register(assetSystem);

            BehaviorUpdateSystem bus = new BehaviorUpdateSystem(game.SystemRegistry);
            game.SystemRegistry.Register(bus);
            bus.Register(imGuiRenderer);

            PhysicsSystem ps = new PhysicsSystem();
            game.SystemRegistry.Register(ps);

            ScoreSystem ss = new ScoreSystem();
            game.SystemRegistry.Register(ss);

            ConsoleCommandSystem ccs = new ConsoleCommandSystem(game.SystemRegistry);
            game.SystemRegistry.Register(ccs);

            window.Closed += game.Exit;

            LooseFileDatabase db = new LooseFileDatabase(AppContext.BaseDirectory);

            AddBinGameScene();
            SceneAsset scene = new SceneAsset();
            var goqs = game.SystemRegistry.GetSystem<GameObjectQuerySystem>();
            scene.GameObjects = goqs.GetAllGameObjects().Select(go => new SerializedGameObject(go)).ToArray();
            db.SaveDefinition(scene, "BINSCENE.scene");
            var loaded = db.LoadAsset<SceneAsset>("BINSCENE.scene");
            db.SaveDefinition(loaded, "BINSCENE.scene");

            //var scene = db.LoadAsset<SceneAsset>("BINSCENE.scene_New");
            //scene.GenerateGameObjects();

            game.RunMainLoop();
        }

        private static void AddBinGameScene()
        {
            GameObject camera = new GameObject("Camera");
            camera.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0.3f, 0f);
            camera.AddComponent(new Camera());

            GameObject character = new GameObject("Character");
            character.AddComponent(new CharacterController());
            character.Transform.Position = new Vector3(0, 0, -5f);

            camera.Transform.Parent = character.Transform;
            camera.Transform.LocalPosition = new Vector3(0, 1.0f, 0);
            camera.AddComponent(new FpsLookController());
            camera.AddComponent(new CameraBob());

            GameObject light = new GameObject("Light");
            var lightComponent = new DirectionalLight(RgbaFloat.White, new Vector3(0.3f, -1f, -1f));
            light.AddComponent(lightComponent);

            var woodTexture = Path.Combine("Textures", "Wood.png");
            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    GameObject plane = new GameObject($"Plane{x},{z}");
                    plane.Transform.Position = new Vector3(-18 + (6 * x), -3.5f, -18 + (6 * z));
                    plane.Transform.Scale = new Vector3(6);
                    plane.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, woodTexture));
                    plane.AddComponent(new BoxCollider(1f, 0.1f / 30f, 1f, -1.0f));
                }
            }

            var bin = Prefabs.CreateBin();

            var scaleBox = new GameObject("ScaleBox");
            scaleBox.Transform.Position = new Vector3(5f, 0f, 0f);
            scaleBox.Transform.Scale = new Vector3(3f);
            scaleBox.AddComponent(new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, woodTexture));
            scaleBox.AddComponent(new BoxCollider(1.0f, 1.0f, 1.0f, 50.0f));

            camera.AddComponent(new BallLauncher());
            camera.AddComponent(new DebugConsole());
        }

        private static void AddObjectRainScene()
        {
            GameObject camera = new GameObject("Camera");
            camera.Transform.Position = new Vector3(0, 0, -5f);
            camera.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0.3f, 0f);
            camera.AddComponent(new Camera());

            GameObject light = new GameObject("Light");
            var lightComponent = new DirectionalLight(RgbaFloat.White, new Vector3(0.3f, -.3f, -1f));
            light.AddComponent(lightComponent);
            float timeFactor = 0.0f;
            light.AddComponent(new DelegateBehavior(
                dt =>
                {
                    timeFactor += dt;
                    var position = new Vector3(
                        (float)(Math.Cos(timeFactor) * 5),
                        6 + (float)Math.Sin(timeFactor) * 2,
                        -(float)(Math.Sin(timeFactor) * 5));
                    lightComponent.Direction = -position;
                }));

            GameObject sphere = new GameObject("Sphere1");
            var stoneTexture = Path.Combine("Textures", "Stone.png");
            sphere.AddComponent(new MeshRenderer(SphereModel.Vertices, SphereModel.Indices, stoneTexture));
            sphere.Transform.Position = new Vector3(0, 2f, 0f);
            sphere.Transform.Scale = new Vector3(1f);
            sphere.AddComponent(new SphereCollider(1.0f, 1f));

            GameObject sphere2 = new GameObject("Sphere2");
            sphere2.AddComponent(new MeshRenderer(SphereModel.Vertices, SphereModel.Indices, stoneTexture));
            sphere2.Transform.Scale = new Vector3(1f);
            sphere2.Transform.Parent = sphere.Transform;
            sphere2.Transform.LocalPosition = new Vector3(-1f, -1f, 0f);
            sphere2.AddComponent(new SphereCollider(1.0f, 1f));

            GameObject sphere3 = new GameObject("Cube3");
            sphere3.AddComponent(new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, stoneTexture));
            sphere3.Transform.Scale = new Vector3(1f);
            sphere3.Transform.Parent = sphere.Transform;
            sphere3.Transform.LocalPosition = new Vector3(1f, -1f, 0f);
            sphere3.AddComponent(new BoxCollider(1.0f, 1.0f, 1.0f, 1.0f));

            var solidBlue = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.Blue },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);

            GameObject upperTrigger = new GameObject("UpperTrigger");
            var mr = new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, solidBlue) { Wireframe = true };
            upperTrigger.AddComponent(mr);
            upperTrigger.Transform.Parent = sphere.Transform;
            upperTrigger.Transform.LocalPosition = new Vector3(0f, 2f, 0f);
            upperTrigger.Transform.Scale = new Vector3(2.0f);
            var triggerBox = new BoxCollider(1.0f, 1.0f, 1.0f, .01f) { IsTrigger = true };
            upperTrigger.AddComponent(triggerBox);
            triggerBox.TriggerEntered += (other) => Console.WriteLine("Upper box triggered with " + other.GameObject.Name);

            GameObject plane = new GameObject("Plane");
            plane.Transform.Position = new Vector3(0, -3.5f, 0f);
            plane.Transform.Scale = new Vector3(30f);
            plane.Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.05f);
            var woodTexture = Path.Combine("Textures", "Wood.png");
            plane.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, woodTexture));
            plane.AddComponent(new BoxCollider(1f, 0.1f / 30f, 1f, -1.0f));

            camera.AddComponent(new ObjectRain());
            camera.AddComponent(new FreeFlyMovement());
        }
    }
}
