using System;
using Veldrid;

namespace Ge
{
    public class GraphicsSystem : GameSystem
    {
        private readonly RenderContext _rc;
        
        public RenderContext Rc => _rc;
        
        public GraphicsSystem(RenderContext rc)
        {
            _rc = rc;
        }
        
        public void RegisterRenderItem(RenderItem ri)
        {
            
        }

        public override void Update()
        {
        }
    }
}