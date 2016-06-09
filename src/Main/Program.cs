using Ge.Behaviors;
using Ge.Graphics;
using System.Runtime.InteropServices;
using Veldrid.Platform;
using Veldrid.Graphics;
using ImGuiNET;
using System.Numerics;
using System.IO;
using System;

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

            window.Closed += game.Exit;

            AddGameObjects();

            game.RunMainLoop();
        }

        private static void AddGameObjects()
        {
            GameObject camera = new GameObject("Camera");
            camera.Transform.Position = new Vector3(0, 0, -5f);
            camera.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0.3f, 0f);
            camera.AddComponent(new DelegateBehavior(dt => ImGui.Text($"Camera forward: {camera.Transform.Forward}")));
            camera.AddComponent(new Camera());

            GameObject light = new GameObject("Light");
            var lightComponent = new DirectionalLight(RgbaFloat.White, new Vector3(0.3f, -.3f, -1f));
            light.AddComponent(lightComponent);

            GameObject cube = new GameObject("Plane");
            cube.Transform.Position = new Vector3(0, 0f, 0f);
            var solidWhite = new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.White },
                1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);
            cube.AddComponent(new MeshRendererComponent(CubeModel.Vertices, CubeModel.Indices, solidWhite));
            camera.AddComponent(new DelegateBehavior(dt => 
            {
                cube.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(
                    Environment.TickCount / 1000f,
                    Environment.TickCount / 600f,
                    Environment.TickCount / 300f);
            }));
            
            GameObject plane = new GameObject("Plane");
            plane.Transform.Position = new Vector3(0, -2f, 0f);
            plane.Transform.Scale = new Vector3(10f);
            plane.AddComponent(new MeshRendererComponent(PlaneModel.Vertices, PlaneModel.Indices, solidWhite));
        }
    }
}
