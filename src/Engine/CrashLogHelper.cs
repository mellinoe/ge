using Engine.Audio;
using Engine.Graphics;
using SharpDX.DXGI;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Veldrid.Graphics;
using Veldrid.Graphics.OpenGL;

namespace Engine
{
    public static class CrashLogHelper
    {
        public static void LogUnhandledException(Exception e, Game game)
        {
            using (var fs = File.CreateText("crashlog.txt"))
            {
                fs.WriteLine(e.ToString());
                fs.WriteLine();
                fs.WriteLine(RuntimeInformation.OSDescription);
                RenderContext renderContext = game.SystemRegistry.GetSystem<GraphicsSystem>().Context;
                string backend = renderContext is OpenGLRenderContext ? "OpenGL" : "Direct3D11";
                fs.WriteLine($"Using {backend} backend.");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    fs.WriteLine("GPU Devices:");
                    var factory = new Factory1();
                    foreach (var adapter in factory.Adapters)
                    {
                        fs.WriteLine(CreateString(adapter.Description));
                    }
                }

                fs.WriteLine($"Resolution: {renderContext.Window.Width}x{renderContext.Window.Height}");

                var audioEngine = game.SystemRegistry.GetSystem<AudioSystem>().Engine;
                fs.WriteLine($"Audio Engine: {audioEngine.GetType()}");
            }
        }

        private static string CreateString(AdapterDescription description)
        {
            return $"{new string(description.Description.TakeWhile(c => c != 0).ToArray())}";
        }
    }
}
