using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Assets;
using Engine.Assets;
using Newtonsoft.Json;
using System;

namespace Engine.Graphics
{
    public class MeshRenderer : Component, BoundsRenderItem
    {
        private static readonly string[] s_opaqueStages = { "Standard" };
        private static readonly string[] s_opaqueWithShadowStages = { "ShadowMap", "Standard" };
        private static readonly string[] s_transparentStages = { "AlphaBlend" };
        private static readonly string[] s_transparentWithShadowStages = { "ShadowMap", "AlphaBlend" };

        private readonly DynamicDataProvider<Matrix4x4> _worldProvider;
        private readonly DependantDataProvider<Matrix4x4> _inverseTransposeWorldProvider;
        private readonly DynamicDataProvider<TintInfo> _tintInfoProvider;
        private readonly ConstantBufferDataProvider[] _perObjectProviders;
        private readonly ConstantBufferDataProvider[] _transparentPerObjectProviders;
        private int _indexCount;
        private RefOrImmediate<TextureData> _textureRef;
        private RefOrImmediate<MeshData> _meshRef;
        private TextureData _texture;
        private MeshData _mesh;
        private BoundingSphere _centeredBoundingSphere;
        private BoundingBox _centeredBoundingBox;
        private TintInfo _baseTint;
        private TintInfo _overrideTint;
        private DynamicDataProvider<MaterialInfo> _materialInfo;
        private readonly TriangleComparer _triangleComparer = new TriangleComparer();
        private bool _isTransparent;
        private bool _initialized;
        private bool _castShadows = true;

        private TriangleIndices[] _triIndices;
        private ushort[] _meshIndices;
        private Vector3[] _meshVertexPositions;

        private GraphicsSystem _gs;
        private AssetDatabase _ad;

        // Serialization Accessors
        public RefOrImmediate<MeshData> Mesh
        {
            get { return _meshRef; }
            set
            {
                _meshRef = value;
                if (_vb != null)
                {
                    RecreateModel();
                }

                _meshIndices = null;
                _meshVertexPositions = null;
            }
        }
        public RefOrImmediate<TextureData> Texture
        {
            get { return _textureRef; }
            set
            {
                _textureRef = value;
                if (_texture != null)
                {
                    RecreateTexture();
                }
            }
        }

        private void RecreateModel()
        {
            //_vb.Dispose();
            //_ib.Dispose();

            _mesh = _meshRef.Get(_ad);
            _vb = _mesh.CreateVertexBuffer(_gs.Context.ResourceFactory);
            CreateIndexBuffer(_isTransparent);
            _centeredBoundingSphere = _mesh.GetBoundingSphere();
            _centeredBoundingBox = _mesh.GetBoundingBox();
        }

        private void RecreateTexture()
        {
            _deviceTexture.Dispose();
            _textureBinding.Dispose();

            _texture = _textureRef.Get(_ad);
            _deviceTexture = _texture.CreateDeviceTexture(_gs.Context.ResourceFactory);
            _textureBinding = _gs.Context.ResourceFactory.CreateShaderTextureBinding(_deviceTexture);
        }

        // Private device resources -- to be disposed.
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private DeviceTexture _deviceTexture;
        private ShaderTextureBinding _textureBinding;

        // Shared device resources
        private Material _regularPassMaterial;
        private Material _regularPassTransparentMaterial;
        private Material _shadowPassMaterial;

        private static RasterizerState s_wireframeRS;
        private static RasterizerState s_noCullRS;

        public bool Wireframe { get; set; } = false;

        public bool DontCullBackFace { get; set; } = false;

        public TintInfo BaseTint { get { return _baseTint; } set { _baseTint = value; UpdateTintProvider(); } }

        [JsonIgnore]
        public TintInfo OverrideTint { get { return _overrideTint; } set { _overrideTint = value; UpdateTintProvider(); } }

        public float Opacity
        {
            get { return _materialInfo.Data.Opacity; }
            set
            {
                float newVal = MathUtil.Clamp(value, 0f, 1f);
                float oldVal = _materialInfo.Data.Opacity;
                if (newVal != oldVal)
                {

                    bool isTransparent = oldVal == 1f;
                    if (newVal < 1f && oldVal == 1f)
                    {
                        MakeTransparent();
                    }
                    else if (newVal == 1f)
                    {
                        MakeOpaque();
                    }

                    _materialInfo.Data = new MaterialInfo(newVal);
                }
            }
        }

        public bool CastShadows
        {
            get { return _castShadows; }
            set { _castShadows = value; }
        }

        private void MakeTransparent()
        {
            Debug.Assert(!_isTransparent);
            _isTransparent = true;
            if (_ib != null)
            {
                CreateIndexBuffer(wasTransparent: false);
            }
        }

        private void MakeOpaque()
        {
            Debug.Assert(_isTransparent);
            _isTransparent = false;
            if (_ib != null)
            {
                CreateIndexBuffer(wasTransparent: true);
            }
        }

        public Matrix4x4 RenderOffset { get; set; } = Matrix4x4.Identity;

        [JsonIgnore]
        public BoundingBox Bounds
        {
            get
            {
                return BoundingBox.Transform(_centeredBoundingBox, RenderOffset * Transform.GetWorldMatrix());
            }
        }

        public MeshRenderer() : this(EngineEmbeddedAssets.CubeModelID, EngineEmbeddedAssets.PinkTextureID) { }

        [JsonConstructor]
        public MeshRenderer(RefOrImmediate<MeshData> meshData, RefOrImmediate<TextureData> texture)
        {
            _worldProvider = new DynamicDataProvider<Matrix4x4>();
            _inverseTransposeWorldProvider = new DependantDataProvider<Matrix4x4>(_worldProvider, CalculateInverseTranspose);
            _tintInfoProvider = new DynamicDataProvider<TintInfo>();
            _materialInfo = new DynamicDataProvider<MaterialInfo>(new MaterialInfo(1.0f));
            _perObjectProviders = new ConstantBufferDataProvider[] { _worldProvider, _inverseTransposeWorldProvider, _tintInfoProvider };
            _transparentPerObjectProviders = new ConstantBufferDataProvider[] { _worldProvider, _inverseTransposeWorldProvider, _tintInfoProvider, _materialInfo };
            Mesh = meshData;
            Texture = texture;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return _initialized ? RenderOrderKey.Create(Vector3.Distance(Transform.Position, cameraPosition), _regularPassMaterial.GetHashCode()) : new RenderOrderKey();
        }

        public IList<string> GetStagesParticipated()
        {
            if (_isTransparent)
            {
                if (_castShadows)
                {
                    return s_transparentWithShadowStages;
                }
                else
                {
                    return s_transparentStages;
                }
            }
            else
            {
                if (_castShadows)
                {
                    return s_opaqueWithShadowStages;
                }
                else
                {
                    return s_opaqueStages;
                }

            }
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (!_initialized)
            {
                return;
            }

            _worldProvider.Data = RenderOffset * GameObject.Transform.GetWorldMatrix();

            rc.SetVertexBuffer(_vb);
            if (Opacity < 1f)
            {
                SortTransparentTriangles();
            }
            rc.SetIndexBuffer(_ib);

            if (pipelineStage == "ShadowMap")
            {
                rc.SetMaterial(_shadowPassMaterial);
                _shadowPassMaterial.ApplyPerObjectInput(_worldProvider);
            }
            else if (pipelineStage == "AlphaBlend")
            {
                rc.SetMaterial(_regularPassTransparentMaterial);
                _regularPassTransparentMaterial.ApplyPerObjectInputs(_transparentPerObjectProviders);
                _regularPassTransparentMaterial.UseTexture(0, _textureBinding);
                _regularPassMaterial.UseTexture(1, _gs.StandardStageDepthView);
                rc.SetBlendState(rc.AlphaBlend);
            }
            else
            {
                Debug.Assert(pipelineStage == "Standard");

                rc.SetMaterial(_regularPassMaterial);
                _regularPassMaterial.ApplyPerObjectInputs(_perObjectProviders);
                _regularPassMaterial.UseTexture(0, _textureBinding);
            }

            var previousRS = rc.RasterizerState;
            if (Wireframe)
            {
                rc.SetRasterizerState(s_wireframeRS);
            }
            else if (DontCullBackFace)
            {
                rc.SetRasterizerState(s_noCullRS);
            }
            else
            {
                rc.SetRasterizerState(rc.DefaultRasterizerState);
            }

            rc.DrawIndexedPrimitives(_indexCount, 0);
            rc.SetRasterizerState(previousRS);
            if (pipelineStage == "AlphaBlend")
            {
                rc.SetBlendState(rc.OverrideBlend);
            }
        }

        private unsafe void SortTransparentTriangles()
        {
            ushort[] indices = GetMeshIndices();

            if (_triIndices == null || _triIndices.Length < indices.Length / 3)
            {
                _triIndices = new TriangleIndices[indices.Length / 3];
                for (int i = 0; i < _triIndices.Length; i++)
                {
                    _triIndices[i] = new TriangleIndices(indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]);
                }
            }

            _triangleComparer.WorldMatrix = Transform.GetWorldMatrix();
            _triangleComparer.CameraPosition = _gs.MainCamera.Transform.Position;
            _triangleComparer.Positions = GetMeshVertexPositions();

            Array.Sort(_triIndices, _triangleComparer);

            fixed (TriangleIndices* indicesPtr = _triIndices)
            {
                _ib.SetIndices(new IntPtr(indicesPtr), IndexFormat.UInt16, sizeof(uint), indices.Length);
            }
        }

        private ushort[] GetMeshIndices()
        {
            return _meshIndices ?? (_meshIndices = _mesh.GetIndices());
        }

        private Vector3[] GetMeshVertexPositions()
        {
            return _meshVertexPositions ?? (_meshVertexPositions = _mesh.GetVertexPositions());
        }

        private class TriangleComparer : IComparer<TriangleIndices>
        {
            public Matrix4x4 WorldMatrix { get; set; }
            public Vector3 CameraPosition { get; set; }
            public Vector3[] Positions { get; set; }

            public int Compare(TriangleIndices x, TriangleIndices y)
            {
                // Another SIMD bug. These intermediate results need to be pulled into local variables
                //  in order to avoid bad SIMD codegen ultimately resulting in invalid comparer results.
                Vector3 cameraPositionLocal = CameraPosition;

                Vector3 posX0 = Positions[x.I0];
                Vector3 posX1 = Positions[x.I1];
                Vector3 posX2 = Positions[x.I2];
                Vector3 xSum = (posX0 + posX1 + posX2);
                Vector3 xAvg = xSum / 3;
                Vector3 xPosition = Vector3.Transform(xAvg, WorldMatrix);

                Vector3 posY0 = Positions[y.I0];
                Vector3 posY1 = Positions[y.I1];
                Vector3 posY2 = Positions[y.I2];
                Vector3 ySum = (posY0 + posY1 + posY2);
                Vector3 yAvg = ySum / 3;
                Vector3 yPosition = Vector3.Transform(yAvg, WorldMatrix);

                float xDistance = Vector3.DistanceSquared(xPosition, cameraPositionLocal);
                float yDistance = Vector3.DistanceSquared(yPosition, cameraPositionLocal);

                return -xDistance.CompareTo(yDistance);
            }
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _ad = registry.GetSystem<AssetSystem>().Database;
            _texture = Texture.Get(_ad);
            _mesh = Mesh.Get(_ad);
            _centeredBoundingSphere = _mesh.GetBoundingSphere();
            _centeredBoundingBox = _mesh.GetBoundingBox();
            _gs.ExecuteOnMainThread(() =>
            {
                InitializeContextObjects(_gs.Context, _gs.MaterialCache, _gs.BufferCache);
            });
        }

        protected override void Removed(SystemRegistry registry)
        {
            ClearDeviceResources();
        }

        protected override void OnEnabled()
        {
            _gs.AddRenderItem(this, Transform);
        }

        protected override void OnDisabled()
        {
            _gs.RemoveRenderItem(this);
        }

        private async void InitializeContextObjects(RenderContext context, MaterialCache materialCache, BufferCache bufferCache)
        {
            ResourceFactory factory = context.ResourceFactory;

            Debug.Assert(_vb == null);
            Debug.Assert(_ib == null);
            Debug.Assert(_deviceTexture == null);
            Debug.Assert(_textureBinding == null);

            _vb = bufferCache.GetVertexBuffer(_mesh);
            CreateIndexBuffer(wasTransparent: false);

            if (s_regularGlobalInputs == null)
            {
                s_regularGlobalInputs = new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement[]
                    {
                        new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                        new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix"),
                        new MaterialGlobalInputElement("LightProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "LightProjMatrix"),
                        new MaterialGlobalInputElement("LightViewMatrixBuffer", MaterialInputType.Matrix4x4, "LightViewMatrix"),
                        new MaterialGlobalInputElement("LightInfoBuffer", MaterialInputType.Custom, "LightBuffer"),
                        new MaterialGlobalInputElement("CameraInfoBuffer", MaterialInputType.Custom, "CameraInfo"),
                        new MaterialGlobalInputElement("PointLightsBuffer", MaterialInputType.Custom, "PointLights")
                    });
            }

            _regularPassMaterial = materialCache.GetMaterial(
                context,
                RegularPassVertexShaderSource,
                RegularPassFragmentShaderSource,
                s_vertexInputs,
                s_regularGlobalInputs,
                s_perObjectInputs,
                s_textureInputs);

            _regularPassTransparentMaterial = materialCache.GetMaterial(
                context,
                RegularPassTransparentVertexShaderSource,
                RegularPassTransparentFragmentShaderSource,
                s_vertexInputs,
                s_regularGlobalInputs,
                s_transparentPerObjectInputs,
                s_transparentTextureInputs);

            if (_texture == null)
            {
                _texture = RawTextureDataArray<RgbaFloat>.FromSingleColor(RgbaFloat.Pink);
            }

            _deviceTexture = await _gs.ExecuteOnMainThread(() => _texture.CreateDeviceTexture(factory));
            _textureBinding = await _gs.ExecuteOnMainThread(() => factory.CreateShaderTextureBinding(_deviceTexture));

            if (s_shadowmapGlobalInputs == null)
            {
                s_shadowmapGlobalInputs = new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement[]
                    {
                        new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "LightProjMatrix"),
                        new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "LightViewMatrix")
                    });
            }

            _shadowPassMaterial = materialCache.GetMaterial(
                context,
                ShadowMapPassVertexShaderSource,
                ShadowMapPassFragmentShaderSource,
                s_vertexInputs,
                s_shadowmapGlobalInputs,
                s_shadowmapPerObjectInputs,
                MaterialTextureInputs.Empty);

            if (s_wireframeRS == null)
            {
                s_wireframeRS = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Wireframe, true, true);
            }
            if (s_noCullRS == null)
            {
                s_noCullRS = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
            }

            _initialized = true;
        }

        private void CreateIndexBuffer(bool wasTransparent)
        {
            var factory = _gs.Context.ResourceFactory;
            var bufferCache = _gs.BufferCache;

            if (wasTransparent) // Transparent meshes do not use shared index buffers.
            {
                _ib?.Dispose();
            }

            if (_isTransparent)
            {
                ushort[] indices = GetMeshIndices();
                _ib = factory.CreateIndexBuffer(indices, IndexFormat.UInt16, true);
                _indexCount = indices.Length;
            }
            else
            {
                var ibAndCount = bufferCache.GetIndexBufferAndCount(_mesh);
                _ib = ibAndCount.Buffer;
                _indexCount = ibAndCount.IndexCount;
            }
        }

        private Matrix4x4 CalculateInverseTranspose(Matrix4x4 m)
        {
            Matrix4x4 inverted;
            Matrix4x4.Invert(m, out inverted);
            return Matrix4x4.Transpose(inverted);
        }

        public void ClearDeviceResources()
        {
            _deviceTexture?.Dispose();
            _textureBinding?.Dispose();
            if (_isTransparent)
            {
                _ib?.Dispose();
            }
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            Vector3 translation, scale; Quaternion rotation;

            bool decomposed = Matrix4x4.Decompose(RenderOffset, out scale, out rotation, out translation);
            var center = _centeredBoundingSphere.Center + translation + Transform.Position;
            var boundingSphere = new BoundingSphere(center, _centeredBoundingSphere.Radius * (decomposed ? scale.X : 1.0f) * Transform.Scale.X);
            return visibleFrustum.Contains(boundingSphere) == ContainmentType.Disjoint;
        }

        private BoundsRenderItemWireframeRenderer _boundsRenderer;
        private bool _boundsRendererEnabled;

        public void ToggleBoundsRenderer()
        {
            _boundsRendererEnabled = !_boundsRendererEnabled;
            if (_boundsRendererEnabled)
            {
                if (_boundsRenderer == null)
                {
                    _boundsRenderer = new BoundsRenderItemWireframeRenderer(this, _gs.Context);
                }

                _gs.AddRenderItem(_boundsRenderer, Transform);
            }

            else
            {
                if (_boundsRenderer != null)
                {
                    _gs.RemoveRenderItem(_boundsRenderer);
                }
            }
        }

        public bool RayCast(Ray ray, out float distance)
        {
            Matrix4x4 invWorld;
            if (!Matrix4x4.Invert(_worldProvider.Data, out invWorld))
            {
                distance = 0f;
                return false;
            }

            ray = Ray.Transform(ray, invWorld);
            bool result = _mesh.RayCast(ray, out distance);
            if (result)
            {
                Vector3 total = ray.Direction * distance;
                distance = (total * Transform.Scale).Length();
            }

            return result;
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            Matrix4x4 invWorld;
            if (!Matrix4x4.Invert(_worldProvider.Data, out invWorld))
            {
                return 0;
            }

            ray = Ray.Transform(ray, invWorld);
            int numHits = _mesh.RayCast(ray, distances);
            for (int i = distances.Count - numHits; i < distances.Count; i++)
            {
                float distance = distances[i];
                Vector3 total = ray.Direction * distance;
                distances[i] = (total * Transform.Scale).Length();
            }
            return numHits;
        }

        private void UpdateTintProvider()
        {
            float factor = _baseTint.TintFactor + ((1 - _baseTint.TintFactor) * _overrideTint.TintFactor);
            Vector3 color = Vector3.Lerp(_baseTint.Color, _overrideTint.Color, 1 - _baseTint.TintFactor);
            _tintInfoProvider.Data = new TintInfo(color, factor);
        }

        private static readonly string RegularPassTransparentVertexShaderSource = "shadow-transparent-vertex";
        private static readonly string RegularPassVertexShaderSource = "shadow-vertex";
        private static readonly string RegularPassFragmentShaderSource = "shadow-frag";
        private static readonly string RegularPassTransparentFragmentShaderSource = "shadow-transparent-frag";

        private static readonly string ShadowMapPassVertexShaderSource = "shadowmap-vertex";
        private static readonly string ShadowMapPassFragmentShaderSource = "shadowmap-frag";

        private static MaterialVertexInput s_vertexInputs = new MaterialVertexInput(
            VertexPositionNormalTexture.SizeInBytes,
            new MaterialVertexInputElement[]
            {
                new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                new MaterialVertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                new MaterialVertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)
            });
        private static MaterialInputs<MaterialGlobalInputElement> s_regularGlobalInputs;
        private static unsafe MaterialInputs<MaterialPerObjectInputElement> s_perObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(
            new MaterialPerObjectInputElement[]
            {
                new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("InverseTransposeWorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("TintInfoBuffer", MaterialInputType.Float4, sizeof(TintInfo))
            });
        private static unsafe MaterialInputs<MaterialPerObjectInputElement> s_transparentPerObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(
            new MaterialPerObjectInputElement[]
            {
                new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("InverseTransposeWorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("TintInfoBuffer", MaterialInputType.Float4, sizeof(TintInfo)),
                new MaterialPerObjectInputElement("MaterialInfoBuffer", MaterialInputType.Float4, sizeof(MaterialInfo)),
            });

        private static MaterialTextureInputs s_textureInputs = new MaterialTextureInputs(
            new MaterialTextureInputElement[]
            {
                new ManualTextureInput("surfaceTexture"),
                new ContextTextureInputElement("ShadowMap")
            });
        private static MaterialTextureInputs s_transparentTextureInputs = new MaterialTextureInputs(
            new MaterialTextureInputElement[]
            {
                new ManualTextureInput("surfaceTexture"),
                new ManualTextureInput("DepthTexture"),
            });
        private static MaterialInputs<MaterialGlobalInputElement> s_shadowmapGlobalInputs;
        private static unsafe MaterialInputs<MaterialPerObjectInputElement> s_shadowmapPerObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(
            new MaterialPerObjectInputElement[]
            {
                new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4))
            });
    }
}
