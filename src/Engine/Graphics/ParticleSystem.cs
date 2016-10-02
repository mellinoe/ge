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
using System.Runtime.InteropServices;
using System.Linq;

namespace Engine.Graphics
{
    public class ParticleSystem : Behavior, BoundsRenderItem
    {
        private static readonly string[] s_stages = { "AlphaBlend" };

        // Actual CPU-side vertex buffer data.
        private RawList<InstanceData> _instanceData;
        // Housekeeping info for particles.
        private RawList<ParticleState> _particleStates;

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
        private readonly DynamicDataProvider<ParticleProperties> _particleProperties
            = new DynamicDataProvider<ParticleProperties>(new ParticleProperties() { Softness = 0.4f, ColorTint = RgbaFloat.White });
        private readonly ConstantBufferDataProvider[] _providers;
        private float _accumulator;
        private IComparer<InstanceData> _cameraDistanceComparer;
        private DepthStencilState _depthStencilState;

        public ParticleSystem()
        {
            _providers = new ConstantBufferDataProvider[] { _worldProvider, _particleProperties };
            _instanceData = new RawList<InstanceData>();
            _particleStates = new RawList<ParticleState>();
        }

        public ParticleSimulationSpace SimulationSpace { get; set; } = ParticleSimulationSpace.Local;

        public float EmissionRate { get; set; } = 1f;

        public float SpawnPeriodSeconds => 1 / EmissionRate;

        public float ParticleLifetime { get; set; } = 5f;

        public bool AlphaFade { get; set; } = true;

        public float Gravity { get; set; } = 0f;

        public RgbaFloat ColorTint
        {
            get { return _particleProperties.Data.ColorTint; }
            set { var props = _particleProperties.Data; props.ColorTint = value; _particleProperties.Data = props; }
        }

        public float Softness
        {
            get { return _particleProperties.Data.Softness; }
            set { var props = _particleProperties.Data; props.Softness = value; _particleProperties.Data = props; }
        }

        public float StartingSize { get; set; } = 1f;

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
                if (_instanceData.Count > 0)
                {
                    return BoundingBox.CreateFromVertices(_instanceData.Select(id => id.Offset).ToArray());
                }
                else
                {
                    return new BoundingBox();
                }
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
            Array.Sort(_instanceData.Elements, _particleStates.Elements, 0, _instanceData.Count, _cameraDistanceComparer);

            _instanceDataVB.SetVertexData(new ArraySegment<InstanceData>(_instanceData.Elements, 0, _instanceData.Count), InstanceData.VertexDescriptor, 0);
            _worldProvider.Data = SimulationSpace == ParticleSimulationSpace.Local ? Transform.GetWorldMatrix() : Matrix4x4.Identity;
            rc.SetVertexBuffer(_instanceDataVB);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInputs(_providers);
            rc.SetTexture(0, _textureBinding);
            rc.SetTexture(1, _gs.StandardStageDepthView);
            rc.SetBlendState(rc.AlphaBlend);
            rc.DepthStencilState = _depthStencilState;
            rc.DrawInstancedPrimitives(1, _instanceData.Count, PrimitiveTopology.PointList);
            rc.SetBlendState(rc.OverrideBlend);
            rc.DepthStencilState = rc.DefaultDepthStencilState;
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
            _cameraDistanceComparer = new CameraDistanceComparer(_gs);
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
                float age = _particleStates[i].Age;
                if (age >= ParticleLifetime)
                {
                    _instanceData.RemoveAt(i);
                    _particleStates.RemoveAt(i);
                    i--;
                }
                else
                {
                    _particleStates.Elements[i].Age += deltaSeconds;

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
            _instanceData.Add(new InstanceData(position, 1f, StartingSize));
            _particleStates.Add(new ParticleState());
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
                    new MaterialVertexInputElement("in_alpha", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float1, VertexElementInputClass.PerInstance, 1),
                    new MaterialVertexInputElement("in_size", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float1, VertexElementInputClass.PerInstance, 1)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, gs, fs);
            ShaderConstantBindings constantBindings = factory.CreateShaderConstantBindings(rc, shaderSet,
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix"),
                    new MaterialGlobalInputElement("CameraInfoBuffer", MaterialInputType.Custom, "CameraInfo")
                    ),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, _worldProvider.DataSizeInBytes),
                    new MaterialPerObjectInputElement("ParticlePropertiesBuffer", MaterialInputType.Custom, _particleProperties.DataSizeInBytes)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet,
                new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture"), new ManualTextureInput("DepthTexture")));
            _material = new Material(rc, shaderSet, constantBindings, textureSlots);
            _depthStencilState = factory.CreateDepthStencilState(true, DepthComparison.LessEqual, true);
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

        // Vertex buffer per-instance data.
        private struct InstanceData
        {
            public const byte SizeInBytes = 20;
            public const byte ElementCount = 3;

            public Vector3 Offset;
            public float Alpha;
            public float Size;

            public InstanceData(Vector3 offset, float alpha, float size)
            {
                Offset = offset;
                Alpha = alpha;
                Size = size;
            }

            public static VertexDescriptor VertexDescriptor => new VertexDescriptor(SizeInBytes, ElementCount);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleProperties : IEquatable<ParticleProperties>
        {
            public RgbaFloat ColorTint;
            public float Softness;
            private readonly Vector3 __unused;

            public bool Equals(ParticleProperties other)
            {
                return other.ColorTint.Equals(ColorTint) && other.Softness.Equals(Softness);
            }
        }

        // CPU-side housekeeping state for individual particles.
        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleState
        {
            public float Age;
            public float Velocity;
        }

        private class CameraDistanceComparer : IComparer<InstanceData>
        {
            private readonly GraphicsSystem _gs;

            public CameraDistanceComparer(GraphicsSystem gs)
            {
                _gs = gs;
            }

            public int Compare(InstanceData id1, InstanceData id2)
            {
                float distance1 = Vector3.Distance(_gs.MainCamera.Transform.Position, id1.Offset);
                float distance2 = Vector3.Distance(_gs.MainCamera.Transform.Position, id2.Offset);

                return distance2.CompareTo(distance1);
            }
        }
    }
}
