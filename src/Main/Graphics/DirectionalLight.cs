using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;

namespace Ge.Graphics
{
    public class DirectionalLight : Component
    {
        private GraphicsSystem _gs;
        private DynamicDataProvider<LightInfo> _lightProvider = new DynamicDataProvider<LightInfo>();

        public override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _gs.Context.DataProviders.Add("LightBuffer", _lightProvider);
        }

        public override void Removed(SystemRegistry registry)
        {
            _gs.Context.DataProviders.Remove("LightBuffer");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightInfo
    {
        public readonly RgbaFloat DiffuseColor;
        public readonly Vector3 Direction;
        private float __buffer;

        public LightInfo(RgbaFloat color, Vector3 direction)
        {
            DiffuseColor = color;
            Direction = direction;

            __buffer = 0;
        }
    }
}
