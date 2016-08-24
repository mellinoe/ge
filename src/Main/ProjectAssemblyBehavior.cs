using System;
using Engine.Behaviors;
using Veldrid.Graphics;
using Engine;
using Veldrid.Assets;

namespace Ge
{
    public class ProjectAssemblyBehavior : Behavior
    {
        public RgbaFloat Color { get; set; }
        public float FloatValue { get; set; }
        public RefOrImmediate<ImageProcessorTexture> Texture { get; set; }
        public RefOrImmediate<ImageProcessorTexture> Texture2 { get; set; }
        public RefOrImmediate<ImageProcessorTexture> Texture3 { get; set; }

        public override void Update(float deltaSeconds)
        {
            Console.WriteLine("TestBehavior");
        }
    }
}
