using Ge.Assets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Graphics.OpenGL;
using Veldrid.Assets;

namespace Ge.Graphics
{
    public unsafe class MeshRenderer : Component, BoundsRenderItem
    {
        private static readonly string[] s_stages = { "ShadowMap", "Standard" };

        private readonly DynamicDataProvider<Matrix4x4> _worldProvider;
        private readonly DependantDataProvider<Matrix4x4> _inverseTransposeWorldProvider;
        private readonly DynamicDataProvider<TintInfo> _tintInfoProvider;
        private readonly ConstantBufferDataProvider[] _perObjectProviders;
        private readonly VertexPositionNormalTexture[] _vertices;
        private readonly int[] _indices;
        private readonly TextureData _texture;
        private readonly BoundingSphere _centeredBoundingSphere;
        private readonly BoundingBox _centeredBoundingBox;

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _regularPassMaterial;
        private Material _shadowPassMaterial;
        private ShaderTextureBinding _textureBinding;

        private static RasterizerState s_wireframeRS;
        private static RasterizerState s_noCullRS;

        public bool Wireframe { get; set; } = false;

        public bool DontCullBackFace { get; set; } = false;

        public TintInfo Tint { get { return _tintInfoProvider.Data; } set { _tintInfoProvider.Data = value; } }

        public Matrix4x4 RenderOffset { get; set; } = Matrix4x4.Identity;

        public BoundingBox Bounds
        {
            get
            {
                return BoundingBox.Transform(_centeredBoundingBox, RenderOffset * Transform.GetWorldMatrix());
            }
        }

        public MeshRenderer(VertexPositionNormalTexture[] vertices, int[] indices, TextureData texture)
        {
            _worldProvider = new DynamicDataProvider<Matrix4x4>();
            _inverseTransposeWorldProvider = new DependantDataProvider<Matrix4x4>(_worldProvider, CalculateInverseTranspose);
            _tintInfoProvider = new DynamicDataProvider<TintInfo>();
            _perObjectProviders = new ConstantBufferDataProvider[] { _worldProvider, _inverseTransposeWorldProvider, _tintInfoProvider };
            _vertices = vertices;
            _indices = indices;
            _texture = texture;
            _centeredBoundingSphere = BoundingSphere.CreateFromPoints(_vertices);
            _centeredBoundingBox = BoundingBox.CreateFromVertices(vertices, Quaternion.Identity, Vector3.Zero, Vector3.One);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(Vector3.Distance(Transform.Position, cameraPosition), _regularPassMaterial.GetHashCode());
        }

        public IEnumerable<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            _worldProvider.Data = RenderOffset * GameObject.Transform.GetWorldMatrix();

            rc.SetVertexBuffer(_vb);
            rc.SetIndexBuffer(_ib);

            if (pipelineStage == "ShadowMap")
            {
                rc.SetMaterial(_shadowPassMaterial);
                _shadowPassMaterial.ApplyPerObjectInput(_worldProvider);
            }
            else
            {
                Debug.Assert(pipelineStage == "Standard");

                rc.SetMaterial(_regularPassMaterial);

                _regularPassMaterial.ApplyPerObjectInputs(_perObjectProviders);
                _regularPassMaterial.UseTexture(0, _textureBinding);
            }

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

            _regularPassMaterial.ApplyPerObjectInputs(_perObjectProviders);
            rc.DrawIndexedPrimitives(_indices.Length, 0);
        }

        public override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _ad = registry.GetSystem<AssetSystem>().Database;
            InitializeContextObjects(_gs.Context, _gs.MaterialCache);
            _gs.AddRenderItem(this, Transform);
        }

        public override void Removed(SystemRegistry registry)
        {
            _gs.RemoveRenderItem(this);
            ClearDeviceResources();
        }

        private unsafe void InitializeContextObjects(RenderContext context, MaterialCache cache)
        {
            ResourceFactory factory = context.ResourceFactory;

            _vb = factory.CreateVertexBuffer(VertexPositionNormalTexture.SizeInBytes * _vertices.Length, false);
            VertexDescriptor desc = new VertexDescriptor(VertexPositionNormalTexture.SizeInBytes, VertexPositionNormalTexture.ElementCount, 0, IntPtr.Zero);
            _vb.SetVertexData(_vertices, desc);

            _ib = factory.CreateIndexBuffer(sizeof(int) * _indices.Length, false);
            _ib.SetIndices(_indices);

            if (s_regularGlobalInputs == null)
            {
                s_regularGlobalInputs = new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement[]
                    {
                        new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                        new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix"),
                        new MaterialGlobalInputElement("LightProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "LightProjMatrix"),
                        new MaterialGlobalInputElement("LightViewMatrixBuffer", MaterialInputType.Matrix4x4, "LightViewMatrix"),
                        new MaterialGlobalInputElement("LightInfoBuffer", MaterialInputType.Custom, "LightBuffer")
                    });
            }

            _regularPassMaterial = cache.GetMaterial(
                context,
                RegularPassVertexShaderSource,
                RegularPassFragmentShaderSource,
                s_vertexInputs,
                s_regularGlobalInputs,
                s_perObjectInputs,
                s_textureInputs);

            _deviceTexture = _texture.CreateDeviceTexture(factory);
            _textureBinding = factory.CreateShaderTextureBinding(_deviceTexture);

            if (s_shadowmapGlobalInputs == null)
            {
                s_shadowmapGlobalInputs = new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement[]
                    {
                        new MaterialGlobalInputElement("ProjectionMatrix", MaterialInputType.Matrix4x4, "LightProjMatrix"),
                        new MaterialGlobalInputElement("ViewMatrix", MaterialInputType.Matrix4x4, "LightViewMatrix")
                    });
            }

            _shadowPassMaterial = cache.GetMaterial(
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
        }

        private Matrix4x4 CalculateInverseTranspose(Matrix4x4 m)
        {
            Matrix4x4 inverted;
            Matrix4x4.Invert(m, out inverted);
            return Matrix4x4.Transpose(inverted);
        }

        public void ClearDeviceResources()
        {
            _vb.Dispose();
            _ib.Dispose();
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
                    _boundsRenderer = new BoundsRenderItemWireframeRenderer(this, _ad, _gs.Context);
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

        private static readonly string RegularPassVertexShaderSource = "shadow-vertex";
        private static readonly string RegularPassFragmentShaderSource = "shadow-frag";

        private static readonly string ShadowMapPassVertexShaderSource = "shadowmap-vertex";
        private static readonly string ShadowMapPassFragmentShaderSource = "shadowmap-frag";
        private DeviceTexture _deviceTexture;

        private static MaterialVertexInput s_vertexInputs = new MaterialVertexInput(
            VertexPositionNormalTexture.SizeInBytes,
            new MaterialVertexInputElement[]
            {
                new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                new MaterialVertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                new MaterialVertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)
            });
        private static MaterialInputs<MaterialGlobalInputElement> s_regularGlobalInputs;
        private static MaterialInputs<MaterialPerObjectInputElement> s_perObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(
            new MaterialPerObjectInputElement[]
            {
                new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("InverseTransposeWorldMatrixBuffer", MaterialInputType.Matrix4x4, sizeof(Matrix4x4)),
                new MaterialPerObjectInputElement("TintInfoBuffer", MaterialInputType.Float4, sizeof(TintInfo))
            });
        private static MaterialTextureInputs s_textureInputs = new MaterialTextureInputs(
            new MaterialTextureInputElement[]
            {
                new ManualTextureInput("surfaceTexture"),
                new ContextTextureInputElement("ShadowMap")
            });
        private static MaterialInputs<MaterialGlobalInputElement> s_shadowmapGlobalInputs;
        private static MaterialInputs<MaterialPerObjectInputElement> s_shadowmapPerObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(
            new MaterialPerObjectInputElement[]
            {
                new MaterialPerObjectInputElement("WorldMatrix", MaterialInputType.Matrix4x4, sizeof(Matrix4x4))
            });
        private GraphicsSystem _gs;
        private LooseFileDatabase _ad;
    }

    public struct TintInfo
    {
        public readonly Vector3 Color;
        public readonly float TintFactor;

        public TintInfo(Vector3 color, float tintFactor)
        {
            Color = color;
            TintFactor = tintFactor;
        }
    }
}
