using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Graphics.Direct3D;
using Veldrid.Graphics.OpenGL;
using Veldrid.Graphics.Pipeline;
using Veldrid.Platform;

namespace Engine.Graphics
{
    public class GraphicsSystem : GameSystem
    {
        private readonly OctreeVisibilityManager _visiblityManager = new OctreeVisibilityManager();

        private readonly Renderer _renderer;
        private readonly PipelineStage[] _pipelineStages;

        internal void AddPointLight(PointLight pointLight)
        {
            _pointLights.Add(pointLight);
            if (_pointLights.Count > PointLightsBuffer.MaxLights)
            {
                Console.WriteLine($"Only {PointLightsBuffer.MaxLights} point lights are supported. PointLight {pointLight} will not be active.");
            }
        }

        internal void RemovePointLight(PointLight pointLight)
        {
            if (!_pointLights.Remove(pointLight))
            {
                throw new InvalidOperationException($"Couldn't remove point light {pointLight}, it wasn't added.");
            }
        }

        private readonly Window _window;
        private readonly Dictionary<BoundsRenderItem, BoundsRenderItemEntry> _boundsRenderItemEntries = new Dictionary<BoundsRenderItem, BoundsRenderItemEntry>();

        private OctreeRenderer<RenderItem> _octreeRenderer;
        private BoundingFrustum _frustum;
        private Camera _mainCamera;

        public ShadowMapStage ShadowMapStage { get; }

        public MaterialCache MaterialCache { get; }

        public RenderContext Context { get; }

        public Camera MainCamera => _mainCamera;

        private DynamicDataProvider<PointLightsBuffer> _pointLightsProvider = new DynamicDataProvider<PointLightsBuffer>();
        private readonly List<PointLight> _pointLights = new List<PointLight>();

        public void SetViewFrustum(ref BoundingFrustum frustum)
        {
            _frustum = frustum;
            ((StandardPipelineStage)_pipelineStages[1]).CameraFrustum = frustum;
            ((StandardPipelineStage)_pipelineStages[2]).CameraFrustum = frustum;
        }

        public void SetMainCamera(Camera camera)
        {
            _mainCamera = camera;
            ShadowMapStage.MainCamera = camera;

            Context.RegisterGlobalDataProvider("ViewMatrix", _mainCamera.ViewProvider);
            Context.RegisterGlobalDataProvider("ProjectionMatrix", _mainCamera.ProjectionProvider);
            Context.RegisterGlobalDataProvider("CameraInfo", _mainCamera.CameraInfoProvider);
        }

        public void SetDirectionalLight(DirectionalLight directionalLight)
        {
            ShadowMapStage.Light = directionalLight;
        }

        public GraphicsSystem(OpenTKWindow window)
        {
            _window = window;
            Context = CreatePlatformDefaultContext(window);
            MaterialCache = new MaterialCache(Context.ResourceFactory);

            ShadowMapStage = new ShadowMapStage(Context);
            _pipelineStages = new PipelineStage[]
            {
                ShadowMapStage,
                new StandardPipelineStage(Context, "Standard"),
                new StandardPipelineStage(Context, "Overlay")
            };
            _renderer = new Renderer(Context, _pipelineStages);

            Context.RegisterGlobalDataProvider("PointLights", _pointLightsProvider);
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

        public void AddFreeRenderItem(RenderItem ri)
        {
            _visiblityManager.AddRenderItem(ri);
        }

        public void RemoveFreeRenderItem(RenderItem ri)
        {
            _visiblityManager.RemoveRenderItem(ri);
        }

        public void AddRenderItem(BoundsRenderItem bri, Transform transform)
        {
            var octreeItem = _visiblityManager.AddRenderItem(bri.Bounds, bri);
            BoundsRenderItemEntry brie = new BoundsRenderItemEntry(bri, transform, octreeItem);
            _boundsRenderItemEntries.Add(bri, brie);
            brie.Transform.TransformChanged += brie.OnTransformChanged;
        }

        public void RemoveRenderItem(BoundsRenderItem bri)
        {
            BoundsRenderItemEntry brie;
            if (!_boundsRenderItemEntries.TryGetValue(bri, out brie))
            {
                throw new InvalidOperationException("Couldn't remove render item " + bri + ". It was not contained in the visibility manager.");
            }

            _visiblityManager.Octree.RemoveItem(brie.BoundsRenderItem);
            brie.Transform.TransformChanged -= brie.OnTransformChanged;

            _boundsRenderItemEntries.Remove(bri);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            float r = 0.8f;
            float g = 0.8f;
            float b = 0.8f;
            Context.ClearColor = new RgbaFloat(r, g, b, 1.0f);

            UpdatePointLightBuffer();

            _visiblityManager.Octree.ApplyPendingMoves();
            _renderer.RenderFrame(_visiblityManager, _mainCamera.Transform.Position);
        }

        private void UpdatePointLightBuffer()
        {
            PointLightsBuffer plb = new PointLightsBuffer();
            plb.NumActivePointLights = Math.Min(PointLightsBuffer.MaxLights, _pointLights.Count);
            for (int i = 0; i < plb.NumActivePointLights; i++)
            {
                plb[i] = _pointLights[i].GetLightInfo();
            }

            _pointLightsProvider.Data = plb;
        }

        public void ToggleOctreeVisualizer()
        {
            if (_octreeRenderer == null)
            {
                _octreeRenderer = new OctreeRenderer<RenderItem>(_visiblityManager.Octree, Context);
                AddFreeRenderItem(_octreeRenderer);
            }
            else
            {
                _octreeRenderer.Dispose();
                RemoveFreeRenderItem(_octreeRenderer);
                _octreeRenderer = null;
            }
        }
    }

    public class BoundsRenderItemEntry
    {
        public BoundsRenderItem BoundsRenderItem { get; private set; }
        public Transform Transform { get; private set; }
        public OctreeItem<RenderItem> OctreeItem { get; private set; }

        public BoundsRenderItemEntry(BoundsRenderItem bri, Transform transform, OctreeItem<RenderItem> oi)
        {
            BoundsRenderItem = bri;
            Transform = transform;
            OctreeItem = oi;
        }

        public void OnTransformChanged(Transform t)
        {
            OctreeItem.Container.MarkItemAsMoved(OctreeItem, BoundsRenderItem.Bounds);
        }
    }
}