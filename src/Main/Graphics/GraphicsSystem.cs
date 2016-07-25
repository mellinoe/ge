using System;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Graphics.Direct3D;
using Veldrid.Graphics.OpenGL;
using Veldrid.Graphics.Pipeline;
using Veldrid.Platform;

namespace Ge.Graphics
{
    public class GraphicsSystem : GameSystem
    {
        private readonly FlatListVisibilityManager _visiblityManager = new FlatListVisibilityManager();
        private readonly Renderer _renderer;
        private readonly PipelineStage[] _pipelineStages;
        private readonly Window _window;

        private BoundingFrustum _frustum;
        private Camera _mainCamera;

        public MaterialCache MaterialCache { get; }

        public RenderContext Context { get; }

        private Camera MainCamera => _mainCamera;

        public void SetViewFrustum(ref BoundingFrustum frustum)
        {
            _frustum = frustum;
            ((StandardPipelineStage)_pipelineStages[1]).CameraFrustum = frustum;
            ((StandardPipelineStage)_pipelineStages[2]).CameraFrustum = frustum;
        }

        public void SetMainCamera(Camera camera)
        {
            if (_mainCamera != null)
            {
                throw new InvalidOperationException("A main camera is already set.");
            }

            _mainCamera = camera;
        }

        public GraphicsSystem(OpenTKWindow window)
        {
            _window = window;
            Context = CreatePlatformDefaultContext(window);
            MaterialCache = new MaterialCache(Context.ResourceFactory);

            _pipelineStages = new PipelineStage[]
            {
                new ShadowMapStage(Context),
                new StandardPipelineStage(Context, "Standard"),
                new StandardPipelineStage(Context, "Overlay")
            };
            _renderer = new Renderer(Context, _pipelineStages);
        }

        private static RenderContext CreatePlatformDefaultContext(OpenTKWindow window)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new D3DRenderContext(window);
            }
            else
            {
                return new OpenGLRenderContext(window, false);
            }
        }

        public void AddRenderItem(RenderItem ri)
        {
            _visiblityManager.AddRenderItem(ri);
        }

        public void RemoveRenderItem(RenderItem ri)
        {
            _visiblityManager.RemoveRenderItem(ri);
        }

        public override void Update(float deltaSeconds)
        {
            float tickCount = Environment.TickCount / 10.0f;
            float r = 0.5f + (0.5f * (float)Math.Sin(tickCount / 300f));
            float g = 0.5f + (0.5f * (float)Math.Sin(tickCount / 750f));
            float b = 0.5f + (0.5f * (float)Math.Sin(tickCount / 50f));
            Context.ClearColor = new RgbaFloat(r, g, b, 1.0f);

            _renderer.RenderFrame(_visiblityManager, _mainCamera.Transform.Position);
        }
    }
}