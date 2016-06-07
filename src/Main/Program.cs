using System;
using System.Linq;
using Veldrid.Graphics;
using Veldrid.Graphics.OpenGL;
using Veldrid.Platform;

namespace Ge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SameThreadWindow window = new SameThreadWindow();
            window.Title = "ge.Main";
            window.Visible = true;
            OpenGLRenderContext rc = new OpenGLRenderContext(window);
            while (window.Visible && window.Exists)
            {
                var snapshot = window.GetInputSnapshot();
                if (snapshot.KeyEvents.Any(ke => ke.Modifiers == ModifierKeys.Alt && ke.Key == OpenTK.Input.Key.F4))
                {
                    window.Close();
                }
                float tickCount = (float)Environment.TickCount / 10.0f;
                float r = 0.5f + (0.5f * (float)Math.Sin(tickCount / 300f));
                float g = 0.5f + (0.5f * (float)Math.Sin(tickCount / 750f));
                float b = 0.5f + (0.5f * (float)Math.Sin(tickCount / 50f));
                rc.ClearColor = new RgbaFloat(r, g, b, 1.0f);
                rc.ClearBuffer();
                rc.SwapBuffers();
            }
        }
    }
}
