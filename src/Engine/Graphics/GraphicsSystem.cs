using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
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
        private static readonly DynamicDataProvider<LightInfo> s_noLightProvider
            = new DynamicDataProvider<LightInfo>(new LightInfo(RgbaFloat.Black, Vector3.Zero));
        private static readonly DynamicDataProvider<Vector4> s_noCameraProvider
            = new DynamicDataProvider<Vector4>();

        private static readonly ConstantDataProvider<Matrix4x4> s_identityProvider = new ConstantDataProvider<Matrix4x4>(Matrix4x4.Identity);

        private readonly Window _window;
        private readonly Renderer _renderer;
        private readonly PipelineStage[] _pipelineStages;
        private readonly OctreeVisibilityManager _visiblityManager = new OctreeVisibilityManager();
        private readonly Dictionary<BoundsRenderItem, BoundsRenderItemEntry> _boundsRenderItemEntries = new Dictionary<BoundsRenderItem, BoundsRenderItemEntry>();
        private readonly DynamicDataProvider<PointLightsBuffer> _pointLightsProvider = new DynamicDataProvider<PointLightsBuffer>();
        private readonly List<PointLight> _pointLights = new List<PointLight>();

        private BoundingFrustum _frustum;
        private Camera _mainCamera;
        private OctreeRenderer<RenderItem> _octreeRenderer;
        public ImGuiRenderer ImGuiRenderer { get; private set; }

        public ShadowMapStage ShadowMapStage { get; }

        public MaterialCache MaterialCache { get; }

        public RenderContext Context { get; }

        public Camera MainCamera => _mainCamera;

        public GraphicsSystem(OpenTKWindow window, bool preferOpenGL = false)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            _window = window;
            Context = CreatePlatformDefaultContext(window, preferOpenGL);
            MaterialCache = new MaterialCache(Context.ResourceFactory);

            ShadowMapStage = new ShadowMapStage(Context);
            _pipelineStages = new PipelineStage[]
            {
                ShadowMapStage,
                new StandardPipelineStage(Context, "Standard"),
                new StandardPipelineStage(Context, "Overlay")
            };
            _renderer = new Renderer(Context, _pipelineStages);

            // Placeholder providers so that materials can bind to them.
            Context.RegisterGlobalDataProvider("ViewMatrix", s_identityProvider);
            Context.RegisterGlobalDataProvider("ProjectionMatrix", s_identityProvider);
            Context.RegisterGlobalDataProvider("CameraInfo", s_noCameraProvider);
            Context.RegisterGlobalDataProvider("LightBuffer", s_noLightProvider);

            Context.RegisterGlobalDataProvider("PointLights", _pointLightsProvider);
            Context.ResourceFactory.AddShaderLoader(new EmbeddedResourceShaderLoader(typeof(GraphicsSystem).GetTypeInfo().Assembly));
        }

        public void SetViewFrustum(ref BoundingFrustum frustum)
        {
            _frustum = frustum;
            ((StandardPipelineStage)_pipelineStages[1]).CameraFrustum = frustum;
            ((StandardPipelineStage)_pipelineStages[2]).CameraFrustum = frustum;
        }

        public void SetImGuiRenderer(ImGuiRenderer imGuiRenderer)
        {
            ImGuiRenderer = imGuiRenderer;
            AddFreeRenderItem(imGuiRenderer);
        }

        public void SetMainCamera(Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            _mainCamera = camera;
            ShadowMapStage.MainCamera = camera;

            Context.RegisterGlobalDataProvider("ViewMatrix", _mainCamera.ViewProvider);
            Context.RegisterGlobalDataProvider("ProjectionMatrix", _mainCamera.ProjectionProvider);
            Context.RegisterGlobalDataProvider("CameraInfo", _mainCamera.CameraInfoProvider);
        }

        public void SetDirectionalLight(DirectionalLight directionalLight)
        {
            if (directionalLight == null)
            {
                throw new ArgumentNullException(nameof(directionalLight));
            }

            ShadowMapStage.Light = directionalLight;
            Context.RegisterGlobalDataProvider("LightBuffer", directionalLight.LightProvider);
        }

        public void UnsetDirectionalLight(DirectionalLight directionalLight)
        {
            if (directionalLight == null)
            {
                throw new ArgumentNullException(nameof(directionalLight));
            }

            if (ShadowMapStage.Light == directionalLight)
            {
                ShadowMapStage.Light = null;
                Context.RegisterGlobalDataProvider("LightBuffer", s_noLightProvider);
            }
        }

        public void AddPointLight(PointLight pointLight)
        {
            if (pointLight == null)
            {
                throw new ArgumentNullException(nameof(pointLight));
            }

            _pointLights.Add(pointLight);
            if (_pointLights.Count > PointLightsBuffer.MaxLights)
            {
                Console.WriteLine($"Only {PointLightsBuffer.MaxLights} point lights are supported. PointLight {pointLight} will not be active.");
            }
        }

        public void RemovePointLight(PointLight pointLight)
        {
            if (pointLight == null)
            {
                throw new ArgumentNullException(nameof(pointLight));
            }

            if (!_pointLights.Remove(pointLight))
            {
                throw new InvalidOperationException($"Couldn't remove point light {pointLight}, it wasn't added.");
            }
        }

        public void AddFreeRenderItem(RenderItem ri)
        {
            if (ri == null)
            {
                throw new ArgumentNullException(nameof(ri));
            }

            _visiblityManager.AddRenderItem(ri);
        }

        public void RemoveFreeRenderItem(RenderItem ri)
        {
            if (ri == null)
            {
                throw new ArgumentNullException(nameof(ri));
            }

            _visiblityManager.RemoveRenderItem(ri);
        }

        public void AddRenderItem(BoundsRenderItem bri, Transform transform)
        {
            if (bri == null)
            {
                throw new ArgumentNullException(nameof(bri));
            }
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            var octreeItem = _visiblityManager.AddRenderItem(bri.Bounds, bri);
            BoundsRenderItemEntry brie = new BoundsRenderItemEntry(bri, transform, octreeItem);
            _boundsRenderItemEntries.Add(bri, brie);
            brie.Transform.TransformChanged += brie.OnTransformChanged;
        }

        public void RemoveRenderItem(BoundsRenderItem bri)
        {
            if (bri == null)
            {
                throw new ArgumentNullException(nameof(bri));
            }

            BoundsRenderItemEntry brie;
            if (!_boundsRenderItemEntries.TryGetValue(bri, out brie))
            {
                throw new InvalidOperationException("Couldn't remove render item " + bri + ". It was not contained in the visibility manager.");
            }

            _visiblityManager.Octree.RemoveItem(brie.BoundsRenderItem);
            brie.Transform.TransformChanged -= brie.OnTransformChanged;

            _boundsRenderItemEntries.Remove(bri);
        }

        public int RayCast(Ray ray, List<RenderItem> hits)
        {
            hits.Clear();
            return _visiblityManager.Octree.RayCast(ray, hits, RayCastFilter);
        }

        private bool RayCastFilter(Ray ray, RenderItem ri)
        {
            float distance;
            return ((BoundsRenderItem)ri).RayCast(ray, out distance);
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

        protected override void UpdateCore(float deltaSeconds)
        {
            UpdatePointLightBuffer();

            _visiblityManager.Octree.ApplyPendingMoves();
            _renderer.RenderFrame(_visiblityManager, _mainCamera.Transform.Position);
        }

        private static RenderContext CreatePlatformDefaultContext(OpenTKWindow window, bool preferOpenGL = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !preferOpenGL)
            {
                return new D3DRenderContext(window);
            }
            else
            {
                return new OpenGLRenderContext(window, false);
            }
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

        private class BoundsRenderItemEntry
        {
            public BoundsRenderItem BoundsRenderItem { get; private set; }
            public Transform Transform { get; private set; }
            public OctreeItem<RenderItem> OctreeItem { get; private set; }

            public BoundsRenderItemEntry(BoundsRenderItem bri, Transform transform, OctreeItem<RenderItem> oi)
            {
                Debug.Assert(bri != null);
                Debug.Assert(transform != null);
                Debug.Assert(oi != null);

                BoundsRenderItem = bri;
                Transform = transform;
                OctreeItem = oi;
            }

            public void OnTransformChanged(Transform t)
            {
                Debug.Assert(t != null);
                OctreeItem.Container.MarkItemAsMoved(OctreeItem, BoundsRenderItem.Bounds);
            }
        }
    }
}