using Ge.Behaviors;
using Ge.Graphics;
using System.Runtime.InteropServices;
using Veldrid.Platform;
using Veldrid.Graphics;
using ImGuiNET;
using System.Numerics;
using System.IO;
using System;
using Ge.Physics;

namespace Ge
{
    public class Program
    {
        private static ObjMeshInfo s_sphereMeshInfo = ObjImporter.LoadFromPath(Path.Combine(AppContext.BaseDirectory, "Models", "Sphere.obj"));
        
        public static void Main(string[] args)
        {
            OpenTKWindow window = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (OpenTKWindow)new DedicatedThreadWindow() : new SameThreadWindow();
            window.Title = "ge.Main";
            window.Visible = true;
            Game game = new Game();
            GraphicsSystem gs = new GraphicsSystem(window);
            game.SystemRegistry.Register(gs);
            ImGuiRenderer imGuiRenderer = new ImGuiRenderer(gs.Context, window.NativeWindow);
            gs.AddRenderItem(imGuiRenderer);

            InputSystem inputSystem = new InputSystem(window);
            inputSystem.RegisterCallback((input) =>
            {
                if (input.GetKeyDown(OpenTK.Input.Key.F4) && (input.GetKey(OpenTK.Input.Key.AltLeft) || input.GetKey(OpenTK.Input.Key.AltRight)))
                {
                    game.Exit();
                }
                if (input.GetKeyDown(OpenTK.Input.Key.F11))
                {
                    window.WindowState = window.WindowState == WindowState.Normal ? WindowState.FullScreen : WindowState.Normal;
                }

                imGuiRenderer.UpdateImGuiInput(window, input.CurrentSnapshot);
                bool limitFramerate = game.LimitFrameRate;
                if (ImGui.Checkbox("Limit Framerate", ref limitFramerate))
                {
                    game.LimitFrameRate = limitFramerate;
                }
            });

            game.SystemRegistry.Register(inputSystem);

            BehaviorUpdateSystem bus = new BehaviorUpdateSystem();
            game.SystemRegistry.Register(bus);
            bus.Register(imGuiRenderer);

            PhysicsSystem ps = new PhysicsSystem();
            game.SystemRegistry.Register(ps);

            window.Closed += game.Exit;

            AddGameObjects();

            game.RunMainLoop();
        }

        private static void AddGameObjects()
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

            GameObject cube = new GameObject("Cube1");
            cube.Transform.Position = new Vector3(0, 0f, 0f);
            var solidWhite = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.White },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
            cube.AddComponent(new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, solidWhite));
            var bc = new BoxCollider(1f, 1f, 1f);
            cube.AddComponent(bc);

            var cube2 = new GameObject("Cube2");
            cube2.Transform.Position = new Vector3(-1.5f, 2.3f, 0f);
            var solidBlue = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.Blue },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
            cube2.AddComponent(new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, solidBlue));
            bc = new BoxCollider(1f, 1f, 1f);
            cube2.AddComponent(bc);

            GameObject sphere = new GameObject("Sphere");
            var stoneTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Stone.png"));
            sphere.AddComponent(new MeshRenderer(s_sphereMeshInfo.Vertices, s_sphereMeshInfo.Indices, stoneTexture));
            sphere.Transform.Position = new Vector3(0, 4f, 0f);
            sphere.Transform.Scale = new Vector3(2f, 2f, 2f);
            sphere.AddComponent(new SphereCollider(2.0f, -1.0f));

            GameObject plane = new GameObject("Plane");
            plane.Transform.Position = new Vector3(0, -3.5f, 0f);
            plane.Transform.Scale = new Vector3(30f);
            var woodTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Wood.png"));
            plane.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, woodTexture));
            plane.AddComponent(new BoxCollider(30.0f, 0.1f, 30.0f, -1.0f));

            float elapsed = 0f;
            Random r = new Random();
            camera.AddComponent(new DelegateBehavior(dt =>
            {
                elapsed += dt;
                const float dropInterval = 0.1f;
                if (elapsed >= dropInterval)
                {
                    elapsed -= dropInterval;
                    //DropRandomObject(r);
                }

                ImGui.Text($"{s_numBoxes} boxes");
            }));

            camera.AddComponent(new FreeFlyMovement());
            camera.AddComponent(new SphereCollider(1.0f, -1f));

            var fta = new FrameTimeAverager(666);
            camera.AddComponent(new DelegateBehavior(dt =>
            {
                fta.AddTime(dt * 1000.0);
                ImGui.Text(fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + fta.CurrentAverageFrameTime.ToString("#00.00 ms"));
            }));
            
            var parent = new GameObject("Parent");
            parent.AddComponent(new BoxCollider(.3f, 5f, .3f, 1.0f));
            parent.AddComponent(new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, woodTexture));
            parent.Transform.Scale = new Vector3(.3f, 5f, .3f);
            parent.Transform.Position = new Vector3(0f, 10f, 0f);
            
            
            var child = new GameObject("Child");
            parent.AddComponent(new BoxCollider(.3f, 5f, .3f, 1.0f));
            parent.AddComponent(new MeshRenderer(s_sphereMeshInfo.Vertices, s_sphereMeshInfo.Indices, woodTexture));
            child.Transform.Parent = parent.Transform;
        }

        private static int s_numBoxes = 2;
        private static void DropRandomObject(Random r)
        {
            var color = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { new RgbaFloat((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()) },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);

            bool isBox = r.NextDouble() <= 0.8;
            var newGo = new GameObject((isBox ? "Cube" : "Sphere") + (++s_numBoxes));
            newGo.Transform.Position = new Vector3((float)r.NextDouble() * 29f - 14f, (float)r.NextDouble() * 10f, (float)r.NextDouble() * 29f - 14f);
            var mr = isBox 
                ? new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, color)
                : new MeshRenderer(s_sphereMeshInfo.Vertices, s_sphereMeshInfo.Indices, color);
            mr.Wireframe = r.NextDouble() > 0.9;
            newGo.AddComponent(mr);
            float radius = 0.3f + (float)r.NextDouble() * .75f;
            if (!isBox)
            {
                newGo.Transform.Scale = new Vector3(radius);
            }
            Collider collider = isBox ? (Collider) new BoxCollider(1f, 1f, 1f) : new SphereCollider(radius);
            
            newGo.AddComponent(collider);
            newGo.AddComponent(new TimedDeath(30.0f));
            newGo.Destroyed += (go) => s_numBoxes--;
            newGo.Transform.Rotation = Quaternion.CreateFromYawPitchRoll((float)r.NextDouble() * 10f, 0f, 0f);
        }
    }
}
