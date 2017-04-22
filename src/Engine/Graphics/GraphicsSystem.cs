using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly DynamicDataProvider<CameraInfo> s_noCameraProvider
            = new DynamicDataProvider<CameraInfo>();

        private static readonly ConstantDataProvider<Matrix4x4> s_identityProvider = new ConstantDataProvider<Matrix4x4>(Matrix4x4.Identity);

        private readonly Window _window;
        private readonly Renderer _renderer;
        private readonly PipelineStage[] _pipelineStages;
        private readonly OctreeVisibilityManager _visiblityManager = new OctreeVisibilityManager();
        private readonly Dictionary<BoundsRenderItem, BoundsRenderItemEntry> _boundsRenderItemEntries = new Dictionary<BoundsRenderItem, BoundsRenderItemEntry>();
        private readonly DynamicDataProvider<PointLightsBuffer> _pointLightsProvider = new DynamicDataProvider<PointLightsBuffer>();
        private readonly List<PointLight> _pointLights = new List<PointLight>();
        private readonly List<float> _rayCastDistances = new List<float>();
        private readonly GraphicsSystemTaskScheduler _taskScheduler;
        private readonly BlockingCollection<BoundsRenderItem> _renderItemsToRemove = new BlockingCollection<BoundsRenderItem>();
        private readonly BlockingCollection<BriTransformPair> _renderItemsToAdd = new BlockingCollection<BriTransformPair>();
        private readonly BlockingCollection<PointLight> _pointLightsToRemove = new BlockingCollection<PointLight>();
        private readonly BlockingCollection<PointLight> _pointLightsToAdd = new BlockingCollection<PointLight>();

        private BoundingFrustum _frustum;
        private Camera _mainCamera;
        private OctreeRenderer<RenderItem> _octreeRenderer;
        private float _preUpscaleQuality = 1.0f;
        private Framebuffer _upscaleSource;
        private Framebuffer _alphaBlendFramebuffer;
        private readonly UpscaleStage _upscaleStage;
        private readonly StandardPipelineStage _standardStage;
        private readonly StandardPipelineStage _overlayStage;
        private readonly StandardPipelineStage _alphaBlendStage;
        private ShaderTextureBinding _upscaleDepthView;
        private bool _needsPreupscaleChange;
        private ManualWireframeRenderer _freeShapeRenderer;
        private bool _freezeLineDrawing;
        private readonly int _mainThreadID;
        private readonly RayCastFilter<RenderItem> _rayCastFilterFunc;

        public ImGuiRenderer ImGuiRenderer { get; private set; }

        public ShadowMapStage ShadowMapStage { get; }

        public MaterialCache MaterialCache { get; }

        public BufferCache BufferCache { get; }

        public RenderContext Context { get; }

        public Camera MainCamera => _mainCamera;

        public float RenderQuality
        {
            get { return _preUpscaleQuality; }
            set
            {
                if (value != _preUpscaleQuality)
                {
                    SetPreupscaleQuality(value);
                }
            }
        }

        public ShaderTextureBinding StandardStageDepthView => _upscaleDepthView;

        public GraphicsSystem(OpenTKWindow window, GraphicsPreferencesProvider preferences)
            : this(window, preferences.RenderQuality, preferences.BackEndPreference)
        { }

        public GraphicsSystem(OpenTKWindow window, float renderQuality = 1f, GraphicsBackEndPreference backEndPreference = GraphicsBackEndPreference.None)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            bool preferOpenGL = backEndPreference == GraphicsBackEndPreference.OpenGL || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _window = window;
            Context = CreatePlatformDefaultContext(window, preferOpenGL);
            Context.ResourceFactory.AddShaderLoader(new EmbeddedResourceShaderLoader(typeof(GraphicsSystem).GetTypeInfo().Assembly));
            MaterialCache = new MaterialCache(this);
            BufferCache = new BufferCache(this);

            ShadowMapStage = new ShadowMapStage(Context);
            _upscaleStage = new UpscaleStage(Context, "Upscale", null, null);
            _standardStage = new StandardPipelineStage(Context, "Standard");
            _alphaBlendStage = new StandardPipelineStage(Context, "AlphaBlend");
            _alphaBlendStage.Comparer = new FarToNearIndexComparer();
            _overlayStage = new StandardPipelineStage(Context, "Overlay");
            _pipelineStages = new PipelineStage[]
            {
                ShadowMapStage,
                _standardStage,
                _alphaBlendStage,
                _upscaleStage,
                _overlayStage,
            };
            _renderer = new Renderer(Context, _pipelineStages);
            SetPreupscaleQuality(renderQuality);

            // Placeholder providers so that materials can bind to them.
            Context.RegisterGlobalDataProvider("ViewMatrix", s_identityProvider);
            Context.RegisterGlobalDataProvider("ProjectionMatrix", s_identityProvider);
            Context.RegisterGlobalDataProvider("CameraInfo", s_noCameraProvider);
            Context.RegisterGlobalDataProvider("LightBuffer", s_noLightProvider);
            Context.RegisterGlobalDataProvider("PointLights", _pointLightsProvider);

            window.Resized += OnWindowResized;

            _freeShapeRenderer = new ManualWireframeRenderer(Context, RgbaFloat.Pink);
            _freeShapeRenderer.Topology = PrimitiveTopology.LineList;
            AddFreeRenderItem(_freeShapeRenderer);

            _mainThreadID = Environment.CurrentManagedThreadId;
            _taskScheduler = new GraphicsSystemTaskScheduler(_mainThreadID);
            _rayCastFilterFunc = RayCastFilter;
        }

        public void SetViewFrustum(ref BoundingFrustum frustum)
        {
            _frustum = frustum;
            _standardStage.CameraFrustum = frustum;
            _alphaBlendStage.CameraFrustum = frustum;
            _overlayStage.CameraFrustum = frustum;
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

            _pointLightsToAdd.Add(pointLight);
        }

        public void RemovePointLight(PointLight pointLight)
        {
            if (pointLight == null)
            {
                throw new ArgumentNullException(nameof(pointLight));
            }
            if (!_pointLights.Contains(pointLight))
            {
                throw new InvalidOperationException($"Couldn't remove point light {pointLight}, it wasn't added.");
            }

            _pointLightsToRemove.Add(pointLight);
        }

        private void CoreAddPointLight(PointLight pointLight)
        {
            _pointLights.Add(pointLight);
            if (_pointLights.Count > PointLightsBuffer.MaxLights)
            {
                Console.WriteLine($"Only {PointLightsBuffer.MaxLights} point lights are supported. PointLight {pointLight} will not be active.");
            }
        }

        private void CoreRemovePointLight(PointLight pointLight)
        {
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

            _renderItemsToAdd.Add(new BriTransformPair() { BoundsRenderItem = bri, Transform = transform });
        }

        private void CoreAddRenderItem(BoundsRenderItem bri, Transform transform)
        {
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

            _renderItemsToRemove.Add(bri);
        }

        private void CoreRemoveRenderItem(BoundsRenderItem bri)
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

        public Task<T> ExecuteOnMainThread<T>(Func<T> func)
        {
            if (Environment.CurrentManagedThreadId == _mainThreadID)
            {
                return Task.FromResult(func());
            }

            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        public Task ExecuteOnMainThread(Action action)
        {
            if (Environment.CurrentManagedThreadId == _mainThreadID)
            {
                action();
                return Task.CompletedTask;
            }

            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        public void NotifyBoundsChanged(BoundsRenderItem bri)
        {
            BoundsRenderItemEntry brie;
            if (!_boundsRenderItemEntries.TryGetValue(bri, out brie))
            {
                throw new InvalidOperationException($"GraphicsSystem was notified of bounds change, but item was not registered. Item: {bri}");
            }

            brie.OnTransformChanged(brie.Transform);
        }

        public int RayCast(Ray ray, List<RayCastHit<RenderItem>> hits)
        {
            hits.Clear();
            return _visiblityManager.Octree.RayCast(ray, hits, _rayCastFilterFunc);
        }

        public void DrawLine(Vector3 start, Vector3 end)
        {
            if (!_freezeLineDrawing)
            {
                _freeShapeRenderer.Vertices.Add(new VertexPositionNormalTexture(start, Vector3.Zero, Vector2.Zero));
                ushort index0 = (ushort)(_freeShapeRenderer.Vertices.Count - 1);
                _freeShapeRenderer.Vertices.Add(new VertexPositionNormalTexture(end, Vector3.Zero, Vector2.Zero));
                ushort index1 = (ushort)(_freeShapeRenderer.Vertices.Count - 1);

                _freeShapeRenderer.Indices.Add(index0);
                _freeShapeRenderer.Indices.Add(index1);
            }
        }

        public void ToggleFreezeLines()
        {
            _freezeLineDrawing = !_freezeLineDrawing;
        }

        private int RayCastFilter(Ray ray, RenderItem ri, List<RayCastHit<RenderItem>> hits)
        {
            _rayCastDistances.Clear();
            int numHits = ((BoundsRenderItem)ri).RayCast(ray, _rayCastDistances);
            if (numHits > 0)
            {
                foreach (float distance in _rayCastDistances)
                {
                    hits.Add(new RayCastHit<RenderItem>(ri, ray.Origin + ray.Direction * distance, distance));
                }
            }

            return numHits;
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
            if (_needsPreupscaleChange)
            {
                SetPreupscaleQuality(_preUpscaleQuality);
                _needsPreupscaleChange = false;
            }
            _taskScheduler.FlushQueuedTasks();
            FlushNewAndRemovedItems();

            UpdatePointLightBuffer();

            _visiblityManager.Octree.ApplyPendingMoves();

            if (_upscaleSource != null)
            {
                Context.SetFramebuffer(_upscaleSource);
                Context.ClearBuffer();
            }
            _renderer.RenderFrame(_visiblityManager, _mainCamera.Transform.Position);

            if (!_freezeLineDrawing)
            {
                _freeShapeRenderer.Clear();
            }
        }

        public void FlushNewAndRemovedItems()
        {
            BriTransformPair pair;
            while (_renderItemsToAdd.TryTake(out pair))
            {
                CoreAddRenderItem(pair.BoundsRenderItem, pair.Transform);
            }

            BoundsRenderItem bri;
            while (_renderItemsToRemove.TryTake(out bri))
            {
                CoreRemoveRenderItem(bri);
            }

            PointLight pl;
            while (_pointLightsToAdd.TryTake(out pl))
            {
                CoreAddPointLight(pl);
            }
            while (_pointLightsToRemove.TryTake(out pl))
            {
                CoreRemovePointLight(pl);
            }
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

        private void SetPreupscaleQuality(float value)
        {
            Debug.Assert(value > 0 && value <= 1);
            _preUpscaleQuality = value;
            int width = (int)(Context.Window.Width * value);
            int height = (int)(width * ((float)Context.Window.Height / Context.Window.Width));
            _upscaleSource?.ColorTexture?.Dispose();
            _upscaleSource?.DepthTexture?.Dispose();
            _upscaleSource?.Dispose();
            _upscaleDepthView?.Dispose();
            _upscaleSource = Context.ResourceFactory.CreateFramebuffer(width, height);

            if (_alphaBlendFramebuffer == null)
            {
                _alphaBlendFramebuffer = Context.ResourceFactory.CreateFramebuffer();
            }
            _alphaBlendFramebuffer.ColorTexture = _upscaleSource.ColorTexture;

            _upscaleDepthView = Context.ResourceFactory.CreateShaderTextureBinding(_upscaleSource.DepthTexture);
            _upscaleStage.SourceTexture = _upscaleSource.ColorTexture;
            _upscaleStage.Enabled = true;

            SetOverrideFramebuffers();
        }

        private void SetOverrideFramebuffers()
        {
            _standardStage.OverrideFramebuffer = _upscaleSource;
            _alphaBlendStage.OverrideFramebuffer = _alphaBlendFramebuffer;
        }

        private void OnWindowResized()
        {
            _needsPreupscaleChange = true;
        }

        private struct BriTransformPair
        {
            public BoundsRenderItem BoundsRenderItem;
            public Transform Transform;
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

        private class GraphicsSystemTaskScheduler : TaskScheduler
        {
            private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
            private readonly int _mainThreadID;

            public GraphicsSystemTaskScheduler(int mainThreadID)
            {
                _mainThreadID = mainThreadID;
            }

            public void FlushQueuedTasks()
            {
                Task t;
                while (_tasks.TryTake(out t))
                {
                    TryExecuteTask(t);
                }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return _tasks.ToArray();
            }

            protected override void QueueTask(Task task)
            {
                _tasks.Add(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return Environment.CurrentManagedThreadId == _mainThreadID && TryExecuteTask(task);
            }

            public void Shutdown()
            {
                _tasks.CompleteAdding();
            }
        }
    }
}