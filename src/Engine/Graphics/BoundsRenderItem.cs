using System.Collections.Generic;
using Veldrid;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public interface BoundsRenderItem : RenderItem
    {
        BoundingBox Bounds { get; }
        bool RayCast(Ray ray, out float distance);
        int RayCast(Ray ray, List<float> distances);
    }
}
