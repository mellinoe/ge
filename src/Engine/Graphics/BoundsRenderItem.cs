using Veldrid;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public interface BoundsRenderItem : RenderItem
    {
        BoundingBox Bounds { get; }
    }
}
