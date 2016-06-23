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
using Ge.Editor;
using System.Collections.Generic;
using System.Diagnostics;

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

            GameObject sphere = new GameObject("Sphere1");
            var stoneTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Stone.png"));
            sphere.AddComponent(new MeshRenderer(s_sphereMeshInfo.Vertices, s_sphereMeshInfo.Indices, stoneTexture));
            sphere.Transform.Position = new Vector3(0, 2f, 0f);
            sphere.Transform.Scale = new Vector3(1f);
            sphere.AddComponent(new SphereCollider(1.0f, 1f));

            GameObject sphere2 = new GameObject("Sphere2");
            sphere2.AddComponent(new MeshRenderer(s_sphereMeshInfo.Vertices, s_sphereMeshInfo.Indices, stoneTexture));
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
            var woodTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Wood.png"));
            plane.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, woodTexture));
            plane.AddComponent(new BoxCollider(1f, 0.1f / 30f, 1f, -1.0f));

            float elapsed = 0f;
            Random r = new Random();
            camera.AddComponent(new DelegateBehavior(dt =>
            {
                elapsed += dt;
                const float dropInterval = 0.1f;
                if (elapsed >= dropInterval)
                {
                    elapsed -= dropInterval;
                    DropRandomObject(r);
                }
            }));

            camera.AddComponent(new FreeFlyMovement());
            camera.AddComponent(new DebugPanel(camera.GetComponent<Camera>()));
        }

        private static int s_totalObjects = 2;
        private static int s_numBoxes = 2;
        private static void DropRandomObject(Random r)
        {
            s_totalObjects++;
            s_numBoxes++;

            var color = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { new RgbaFloat((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()) },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);

            bool isBox = r.NextDouble() <= 0.8;
            var newGo = new GameObject((isBox ? "Cube" : "Sphere") + (s_totalObjects));
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
            Collider collider = isBox ? (Collider)new BoxCollider(1f, 1f, 1f) : new SphereCollider(1.0f);

            newGo.AddComponent(collider);
            newGo.AddComponent(new TimedDeath(30.0f));
            newGo.Destroyed += (go) => s_numBoxes--;
            newGo.Transform.Rotation = Quaternion.CreateFromYawPitchRoll((float)r.NextDouble() * 10f, 0f, 0f);
        }
    }
}
