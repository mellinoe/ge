using Veldrid;
using Veldrid.Graphics;

namespace Ge.Graphics
{
    public interface BoundsRenderItem : RenderItem
    {
        BoundingBox Bounds { get; }
    }
}
