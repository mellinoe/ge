using System;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class PointLight : Component
    {
        private GraphicsSystem _gs;

        public RgbaFloat Color { get; set; } = RgbaFloat.White;
        public float Range { get; set; } = 10f;
        public float Intensity { get; set; } = 1f;

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
        }

        protected override void OnEnabled()
        {
            _gs.AddPointLight(this);
        }

        protected override void OnDisabled()
        {
            _gs.RemovePointLight(this);
        }

        protected override void Removed(SystemRegistry registry)
        {
        }

        public PointLightInfo GetLightInfo()
        {
            return new PointLightInfo(Transform.Position, Color.ToVector4().XYZ(), Range, Intensity);
        }
    }
}
