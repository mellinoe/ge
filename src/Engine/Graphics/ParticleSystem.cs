using Engine.Assets;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class ParticleSystem : Component, BoundsRenderItem
    {
        private static readonly string[] s_stages = { "AlphaBlend" };
        private float _extents = 1f;
        private InstanceData[] _instanceData;

        private GraphicsSystem _gs;
        private AssetDatabase _ad;
        private RefOrImmediate<TextureData> _textureRef;
        private TextureData _texture;

        private VertexBuffer _instanceDataVB;
        private IndexBuffer _ib;
        private Material _material;
        private DeviceTexture _deviceTexture;
        private ShaderTextureBinding _textureBinding;
        private readonly DynamicDataProvider<Matrix4x4> _worldProvider = new DynamicDataProvider<Matrix4x4>();

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

        private void RecreateTexture()
        {
            _deviceTexture.Dispose();
            _textureBinding.Dispose();

            _texture = _textureRef.Get(_ad);
            _deviceTexture = _texture.CreateDeviceTexture(_gs.Context.ResourceFactory);
            _textureBinding = _gs.Context.ResourceFactory.CreateShaderTextureBinding(_deviceTexture);
        }


        public BoundingBox Bounds
        {
            get
            {
                Vector3 center = Transform.Position;
                return new BoundingBox(center - Vector3.One * _extents, center + Vector3.One * _extents);
            }
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(Bounds) == ContainmentType.Disjoint;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return RenderOrderKey.Create(Vector3.Distance(viewPosition, Transform.Position), _material.GetHashCode());
        }

        public IEnumerable<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public bool RayCast(Ray ray, out float distance)
        {
            distance = 0f;
            return false;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            _worldProvider.Data = Transform.GetWorldMatrix();
            rc.SetVertexBuffer(_instanceDataVB);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInput(_worldProvider);
            rc.SetTexture(0, _textureBinding);
            rc.SetBlendState(rc.AlphaBlend);
            rc.DrawInstancedPrimitives(1, _instanceData.Length, PrimitiveTopology.PointList);
            rc.SetBlendState(rc.OverrideBlend);
        }

        protected override void OnEnabled()
        {
            _gs.AddRenderItem(this, Transform);
        }

        protected override void OnDisabled()
        {
            _gs.RemoveRenderItem(this);
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _ad = registry.GetSystem<AssetSystem>().Database;
            _texture = Texture.Get(_ad);
            _instanceData = new[]
            {
                new InstanceData(new Vector3(-3, 0, 0), 1.0f),
                new InstanceData(new Vector3(3, 0, 0), 1.0f)
            };

            InitializeContextObjects(_gs.Context, _gs.MaterialCache, _gs.BufferCache);
        }

        private void InitializeContextObjects(RenderContext rc, MaterialCache materialCache, BufferCache bufferCache)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _instanceDataVB = factory.CreateVertexBuffer(_instanceData, new VertexDescriptor(InstanceData.SizeInBytes, InstanceData.ElementCount), true);
            _ib = factory.CreateIndexBuffer(new[] { 0 }, false);

            if (_texture == null)
            {
                _texture = RawTextureDataArray<RgbaFloat>.FromSingleColor(RgbaFloat.Pink);
            }

            _deviceTexture = _texture.CreateDeviceTexture(factory);
            _textureBinding = factory.CreateShaderTextureBinding(_deviceTexture);

            Shader vs = factory.CreateShader(ShaderType.Vertex, "passthrough-vertex");
            Shader gs = factory.CreateShader(ShaderType.Geometry, "billboard-geometry");
            Shader fs = factory.CreateShader(ShaderType.Fragment, "particle-fragment");
            VertexInputLayout inputLayout = factory.CreateInputLayout(vs,
                new MaterialVertexInput(InstanceData.SizeInBytes,
                    new MaterialVertexInputElement("in_offset", VertexSemanticType.Position, VertexElementFormat.Float3, VertexElementInputClass.PerInstance, 1),
                    new MaterialVertexInputElement("in_alpha", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float1, VertexElementInputClass.PerInstance, 1)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, gs, fs);
            ShaderConstantBindings constantBindings = factory.CreateShaderConstantBindings(rc, shaderSet,
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix"),
                    new MaterialGlobalInputElement("CameraInfoBuffer", MaterialInputType.Custom, "CameraInfo")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, _worldProvider.DataSizeInBytes)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet,
                new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture")));

            _material = new Material(rc, shaderSet, constantBindings, textureSlots);
        }

        protected override void Removed(SystemRegistry registry)
        {
            ClearDeviceResources();
        }

        private void ClearDeviceResources()
        {
            _instanceDataVB.Dispose();
            _ib.Dispose();
            _material.Dispose();
            _textureBinding.Dispose();
        }

        private struct InstanceData
        {
            public const byte SizeInBytes = 16;
            public const byte ElementCount = 2;

            public readonly Vector3 Offset;
            public float Alpha;

            public InstanceData(Vector3 offset, float alpha)
            {
                Offset = offset;
                Alpha = alpha;
            }
        }
    }
}
