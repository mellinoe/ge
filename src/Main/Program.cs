using Ge.Behaviors;
using Ge.Graphics;
using System.Runtime.InteropServices;
using Veldrid.Platform;

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

            game.RunMainLoop();
        }
    }
}
