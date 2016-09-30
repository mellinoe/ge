using BEPUutilities.DataStructures;
using Engine.Assets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;
using System;
using Engine.Behaviors;

namespace Engine.Graphics
{
    public class ParticleSystem : Behavior, BoundsRenderItem
    {
        private static readonly string[] s_stages = { "AlphaBlend" };
        private float _extents = 1f;
        private RawList<InstanceData> _instanceData;
        private RawList<float> _ages;

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
        private readonly DynamicDataProvider<RgbaFloat> _colorTintProvider = new DynamicDataProvider<RgbaFloat>(RgbaFloat.White);
        private readonly ConstantBufferDataProvider[] _providers;
        private float _accumulator;

        public ParticleSystem()
        {
            _providers = new ConstantBufferDataProvider[] { _worldProvider, _colorTintProvider };
            _instanceData = new RawList<InstanceData>();
            _ages = new RawList<float>();
        }

        public ParticleSimulationSpace SimulationSpace { get; set; } = ParticleSimulationSpace.Local;

        public float EmissionRate { get; set; } = 1f;

        public float SpawnPeriodSeconds => 1 / EmissionRate;

        public float ParticleLifetime { get; set; } = 5f;

        public bool AlphaFade { get; set; } = true;

        public float Gravity { get; set; } = 0f;

        public RgbaFloat ColorTint
        {
            get { return _colorTintProvider.Data; }
            set { _colorTintProvider.Data = value; }
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

        [JsonIgnore]
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
            _instanceDataVB.SetVertexData(new ArraySegment<InstanceData>(_instanceData.Elements, 0, _instanceData.Count), InstanceData.VertexDescriptor, 0);
            _worldProvider.Data = SimulationSpace == ParticleSimulationSpace.Local ? Transform.GetWorldMatrix() : Matrix4x4.Identity;
            rc.SetVertexBuffer(_instanceDataVB);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInputs(_providers);
            rc.SetTexture(0, _textureBinding);
            rc.SetBlendState(rc.AlphaBlend);
            rc.DrawInstancedPrimitives(1, _instanceData.Count, PrimitiveTopology.PointList);
            rc.SetBlendState(rc.OverrideBlend);
        }

        protected override void PostEnabled()
        {
            _gs.AddRenderItem(this, Transform);
        }

        protected override void PostDisabled()
        {
            _gs.RemoveRenderItem(this);
        }

        protected override void PostAttached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _ad = registry.GetSystem<AssetSystem>().Database;
            _texture = Texture.Get(_ad);

            InitializeContextObjects(_gs.Context, _gs.MaterialCache, _gs.BufferCache);
        }

        public override void Update(float deltaSeconds)
        {
            _accumulator += deltaSeconds;
            while (_accumulator >= SpawnPeriodSeconds)
            {
                _accumulator -= SpawnPeriodSeconds;
                SpawnParticle();
            }

            for (int i = 0; i < _instanceData.Count; i++)
            {
                float age = _ages[i];
                if (age >= ParticleLifetime)
                {
                    _instanceData.RemoveAt(i);
                    _ages.RemoveAt(i);
                    i--;
                }
                else
                {
                    _ages.Elements[i] += deltaSeconds;
                    float alpha = 1f;
                    if (AlphaFade)
                    {
                        alpha = 1f - (age / ParticleLifetime);
                    }

                    _instanceData.Elements[i].Alpha = alpha;
                    Vector3 offset = _instanceData.Elements[i].Offset;
                    offset += Vector3.UnitY * -10f * Gravity * deltaSeconds;
                    _instanceData.Elements[i].Offset = offset;
                }
            }
        }

        private void SpawnParticle()
        {
            Vector3 position = SimulationSpace == ParticleSimulationSpace.Global ? Transform.Position : Vector3.Zero;
            _instanceData.Add(new InstanceData(position, 1f));
            _ages.Add(0);
        }

        private void InitializeContextObjects(RenderContext rc, MaterialCache materialCache, BufferCache bufferCache)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _instanceDataVB = factory.CreateVertexBuffer(InstanceData.SizeInBytes * 10, true);
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
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, _worldProvider.DataSizeInBytes),
                    new MaterialPerObjectInputElement("ColorTintBuffer", MaterialInputType.Custom, _colorTintProvider.DataSizeInBytes)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet,
                new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture")));

            _material = new Material(rc, shaderSet, constantBindings, textureSlots);
        }

        protected override void PostRemoved(SystemRegistry registry)
        {
            ClearDeviceResources();
        }

        private void RecreateTexture()
        {
            _deviceTexture.Dispose();
            _textureBinding.Dispose();

            _texture = _textureRef.Get(_ad);
            _deviceTexture = _texture.CreateDeviceTexture(_gs.Context.ResourceFactory);
            _textureBinding = _gs.Context.ResourceFactory.CreateShaderTextureBinding(_deviceTexture);
        }

        private void ClearDeviceResources()
        {
            _instanceDataVB.Dispose();
            _ib.Dispose();
            _material.Dispose();
            _textureBinding.Dispose();
        }

        public enum ParticleSimulationSpace
        {
            Local,
            Global,
        }

        private struct InstanceData
        {
            public const byte SizeInBytes = 16;
            public const byte ElementCount = 2;

            public Vector3 Offset;
            public float Alpha;

            public InstanceData(Vector3 offset, float alpha)
            {
                Offset = offset;
                Alpha = alpha;
            }

            public static VertexDescriptor VertexDescriptor => new VertexDescriptor(SizeInBytes, ElementCount);
        }
    }
}
