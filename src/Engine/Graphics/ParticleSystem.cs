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
using Veldrid.Collections;

namespace Engine.Graphics
{
    public class ParticleSystem : Behavior, BoundsRenderItem
    {
        private static readonly string[] s_stages = { "AlphaBlend" };

        private readonly Random _random = new Random();

        // Actual CPU-side vertex buffer data.
        private NativeList<InstanceData> _instanceData;
        // Housekeeping info for particles.
        private NativeList<ParticleStateInternal> _particleStates;

        private Vector3 _currentMinParticleOffset;
        private Vector3 _currentMaxParticleOffset;

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
        private readonly DynamicDataProvider<ParticleSystemGlobalProperties> _particleProperties
            = new DynamicDataProvider<ParticleSystemGlobalProperties>(new ParticleSystemGlobalProperties() { Softness = 0.4f, ColorTint = RgbaFloat.White });
        private readonly ConstantBufferDataProvider[] _providers;
        private float _accumulator;
        private CameraDistanceComparer _cameraDistanceComparer;
        private DepthStencilState _depthStencilState;
        private bool _initialized;

        private static readonly MaterialVertexInput s_vertexInputs =
            new MaterialVertexInput(InstanceData.SizeInBytes,
                new MaterialVertexInputElement("in_offset", VertexSemanticType.Position, VertexElementFormat.Float3, VertexElementInputClass.PerInstance, 1),
                new MaterialVertexInputElement("in_alpha", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float1, VertexElementInputClass.PerInstance, 1),
                new MaterialVertexInputElement("in_size", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float1, VertexElementInputClass.PerInstance, 1));
        private static readonly MaterialInputs<MaterialGlobalInputElement> s_globalInputs =
            new MaterialInputs<MaterialGlobalInputElement>(
                new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix"),
                new MaterialGlobalInputElement("CameraInfoBuffer", MaterialInputType.Custom, "CameraInfo"));
        private static readonly MaterialInputs<MaterialPerObjectInputElement> s_perObjectInputs =
            new MaterialInputs<MaterialPerObjectInputElement>(
                new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, 64),
                new MaterialPerObjectInputElement("ParticlePropertiesBuffer", MaterialInputType.Custom, ParticleSystemGlobalProperties.SizeInBytes));
        private static readonly MaterialTextureInputs s_textureInputs =
            new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture"), new ManualTextureInput("DepthTexture"));

        public ParticleSystem()
        {
            _providers = new ConstantBufferDataProvider[] { _worldProvider, _particleProperties };
            _instanceData = new NativeList<InstanceData>();
            _particleStates = new NativeList<ParticleStateInternal>();
        }

        public ParticleSimulationSpace SimulationSpace { get; set; } = ParticleSimulationSpace.Local;

        public float EmissionRate { get; set; } = 1f;

        public float SpawnPeriodSeconds => 1 / EmissionRate;

        public float ParticleLifetime { get; set; } = 5f;

        public bool AlphaFade { get; set; } = true;

        public float Gravity { get; set; } = 0f;

        public ParticleEmissionShape EmissionShape { get; set; } = ParticleEmissionShape.Sphere;

        public float EmissionShapeSize { get; set; } = 1f;

        public float InitialSpeed { get; set; } = 2f;

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
                if (_instanceData.Count > 1)
                {
                    Vector3 min = _currentMinParticleOffset;
                    Vector3 max = _currentMaxParticleOffset;
                    if (SimulationSpace == ParticleSimulationSpace.Local)
                    {
                        min = Vector3.Transform(min, Transform.GetWorldMatrix());
                        max = Vector3.Transform(max, Transform.GetWorldMatrix());
                    }

                    return new BoundingBox(min, max);
                }
                else
                {
                    return new BoundingBox(Transform.Position - Vector3.One * .1f, Transform.Position + Vector3.One * .1f);
                }
            }
        }

        public void EmitParticles(int numParticles)
        {
            for (int i = 0; i < numParticles; i++)
            {
                SpawnParticle();
            }
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(Bounds) == ContainmentType.Disjoint;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return _initialized ? RenderOrderKey.Create(Vector3.Distance(viewPosition, Transform.Position), _material.GetHashCode()) : new RenderOrderKey();
        }

        public IList<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public bool RayCast(Ray ray, out float distance)
        {
            distance = 0f;
            return false;
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            return 0;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (!_initialized)
            {
                return;
            }

            _cameraDistanceComparer.UpdateCameraPosition();
            NativeList.Sort(_instanceData, _particleStates, 0, _instanceData.Count, _cameraDistanceComparer);

            _instanceDataVB.SetVertexData(_instanceData.Data, InstanceData.VertexDescriptor, (int)_instanceData.Count);
            _worldProvider.Data = SimulationSpace == ParticleSimulationSpace.Local ? Transform.GetWorldMatrix() : Matrix4x4.Identity;
            rc.SetVertexBuffer(_instanceDataVB);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInputs(_providers);
            rc.SetTexture(0, _textureBinding);
            rc.SetTexture(1, _gs.StandardStageDepthView);
            rc.SetBlendState(rc.AlphaBlend);
            rc.DepthStencilState = _depthStencilState;
            rc.DrawInstancedPrimitives(1, (int)_instanceData.Count, PrimitiveTopology.PointList);
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
            _gs.ExecuteOnMainThread(() => InitializeContextObjects(_gs.Context, _gs.MaterialCache, _gs.BufferCache));
        }

        public override void Update(float deltaSeconds)
        {
            if (!_initialized || _instanceData.IsDisposed)
            {
                return;
            }

            _accumulator += deltaSeconds;
            while (_accumulator >= SpawnPeriodSeconds)
            {
                _accumulator -= SpawnPeriodSeconds;
                SpawnParticle();
            }

            _currentMinParticleOffset = new Vector3(float.MaxValue);
            _currentMaxParticleOffset = new Vector3(float.MinValue);
            for (uint i = 0; i < _instanceData.Count; i++)
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
                    _particleStates[i].Age += deltaSeconds;

                    float alpha = 1f;
                    if (AlphaFade)
                    {
                        alpha = 1f - (age / ParticleLifetime);
                    }
                    _instanceData[i].Alpha = alpha;

                    if (Gravity != 0f)
                    {
                        _particleStates[i].Velocity += Vector3.UnitY * -10f * deltaSeconds * Gravity;
                    }

                    _instanceData[i].Offset += _particleStates[i].Velocity * deltaSeconds;

                    _currentMinParticleOffset = Vector3.Min(_currentMinParticleOffset, _instanceData[i].Offset);
                    _currentMaxParticleOffset = Vector3.Max(_currentMaxParticleOffset, _instanceData[i].Offset);
                }
            }

            EnsureMinMaxRange();
            _gs.NotifyBoundsChanged(this);
        }

        private void EnsureMinMaxRange()
        {
            EnsureRange(ref _currentMinParticleOffset.X, ref _currentMaxParticleOffset.X, 0.1f);
            EnsureRange(ref _currentMinParticleOffset.Y, ref _currentMaxParticleOffset.Y, 0.1f);
            EnsureRange(ref _currentMinParticleOffset.Z, ref _currentMaxParticleOffset.Z, 0.1f);
        }

        private void EnsureRange(ref float min, ref float max, float range)
        {
            if (Math.Abs(max - min) < range)
            {
                min -= (range / 2);
                max += (range / 2);
            }
        }

        public void SpawnParticle()
        {
            Vector3 position = Vector3.Zero;
            Vector3 initialVelocity = Vector3.Zero;
            Vector3 emissionDirection = Vector3.Zero;

            switch (EmissionShape)
            {
                case ParticleEmissionShape.Sphere:
                    {
                        emissionDirection = GetRandomPointOnSphere();
                        break;
                    }
                case ParticleEmissionShape.Hemisphere:
                    {
                        emissionDirection = GetRandomPointOnSphere();
                        emissionDirection.Y = Math.Abs(emissionDirection.Y);
                        break;
                    }
                case ParticleEmissionShape.Ring:
                    {
                        Vector2 circlePoint = GetRandomPointOnCircle();
                        emissionDirection = new Vector3(circlePoint.X, 0, circlePoint.Y);
                        break;
                    }
                default:
                    throw new InvalidOperationException("Invalid emission shape: " + EmissionShape);
            }

            emissionDirection = Vector3.Normalize(Vector3.Transform(emissionDirection, Transform.Rotation));

            if (SimulationSpace == ParticleSimulationSpace.Global)
            {
                position = Vector3.Transform(position, Transform.GetWorldMatrix());
            }

            if (emissionDirection != Vector3.Zero)
            {
                position += emissionDirection * EmissionShapeSize * Transform.Scale;
                initialVelocity = Vector3.Normalize(emissionDirection) * InitialSpeed;
            }

            _instanceData.Add(new InstanceData(position, 1f, StartingSize));
            _particleStates.Add(new ParticleStateInternal() { Velocity = initialVelocity });
        }

        public uint GetParticleCount() => _instanceData.Count;

        public ParticleState GetParticle(int index)
        {
            if (index >= GetParticleCount())
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            InstanceData instanceData = _instanceData[index];
            ParticleStateInternal internalState = _particleStates[index];

            return new ParticleState(instanceData.Offset, internalState.Velocity, instanceData.Alpha, instanceData.Size, internalState.Age);
        }

        public void SetParticle(int index, ParticleState state)
        {
            if (index >= GetParticleCount())
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            InstanceData id = new InstanceData(state.Offset, state.Alpha, state.Size);
            _instanceData[index] = id;
            // _instanceData[index].Offset = state.Offset;
            // _instanceData[index].Alpha = state.Alpha;
            // _instanceData[index].Size = state.Size;

            ParticleStateInternal psi = new ParticleStateInternal();
            psi.Velocity = state.Velocity;
            psi.Age = state.Age;
            _particleStates[index] = psi;
            // _particleStates[index].Velocity = state.Velocity;
            // _particleStates[index].Age = state.Age;
        }

        public void ModifyAllParticles(ParticleModifier modifier)
        {
            for (int i = 0; i < GetParticleCount(); i++)
            {
                var particleState = GetParticle(i);
                modifier(ref particleState);
                SetParticle(i, particleState);
            }
        }

        private Vector3 GetRandomPointOnSphere()
        {
            // http://mathworld.wolfram.com/SpherePointPicking.html
            double x1 = _random.NextDouble() * 2 - 1;
            double x2 = _random.NextDouble() * 2 - 1;
            while (x1 * x1 + x2 * x2 >= 1)
            {
                x1 = _random.NextDouble() * 2 - 1;
                x2 = _random.NextDouble() * 2 - 1;
            }

            float x = (float)(2 * x1 * Math.Sqrt(1 - (x1 * x1) - (x2 * x2)));
            float y = (float)(2 * x2 * Math.Sqrt(1 - (x1 * x1) - (x2 * x2)));
            float z = (float)(1 - 2 * ((x1 * x1) + (x2 * x2)));
            return new Vector3(x, y, z);
        }

        private Vector2 GetRandomPointOnCircle()
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
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

            _material = materialCache.GetMaterial(rc,
                "passthrough-vertex", "billboard-geometry", "particle-fragment",
                s_vertexInputs,
                s_globalInputs,
                s_perObjectInputs,
                s_textureInputs);
            _depthStencilState = factory.CreateDepthStencilState(true, DepthComparison.LessEqual, true);

#if DEBUG_PARTICLE_BOUNDS
            var briwr = new BoundsRenderItemWireframeRenderer(this, rc);
            _gs.AddRenderItem(briwr, Transform);
#endif

            _initialized = true;
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
            _instanceDataVB?.Dispose();
            _ib?.Dispose();
            _textureBinding?.Dispose();
            _instanceData.Dispose();
            _particleStates.Dispose();
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

        // GPU storage of global per-system information.
        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleSystemGlobalProperties : IEquatable<ParticleSystemGlobalProperties>
        {
            public RgbaFloat ColorTint;
            public float Softness;
            private readonly Vector3 __unused;

            public const byte SizeInBytes = 32;

            public bool Equals(ParticleSystemGlobalProperties other)
            {
                return other.ColorTint.Equals(ColorTint) && other.Softness.Equals(Softness);
            }
        }

        // CPU-side housekeeping state for individual particles.
        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleStateInternal
        {
            public float Age;
            public Vector3 Velocity;
        }

        private class CameraDistanceComparer : IComparer<InstanceData>
        {
            private readonly GraphicsSystem _gs;
            private Vector3 _currentCameraPosition;

            public CameraDistanceComparer(GraphicsSystem gs)
            {
                _gs = gs;
            }

            public void UpdateCameraPosition()
            {
                _currentCameraPosition = _gs.MainCamera.Transform.Position;
            }

            public int Compare(InstanceData id1, InstanceData id2)
            {
                float distance1 = Vector3.DistanceSquared(_currentCameraPosition, id1.Offset);
                float distance2 = Vector3.DistanceSquared(_currentCameraPosition, id2.Offset);

                return distance2.CompareTo(distance1);
            }
        }
    }

    public enum ParticleSimulationSpace
    {
        Local,
        Global,
    }

    public enum ParticleEmissionShape
    {
        Sphere,
        Hemisphere,
        Ring,
    }

    public delegate void ParticleModifier(ref ParticleState state);

    public struct ParticleState
    {
        /// <summary>
        /// The local offset of the particle. Controls the world-space position of the particle
        /// based on the value of <see cref="ParticleSystem.SimulationSpace"/>.
        /// </summary>
        public Vector3 Offset;
        public Vector3 Velocity;
        public float Alpha;
        public float Size;
        public float Age;

        public ParticleState(Vector3 offset, Vector3 velocity, float alpha, float size, float age)
        {
            Offset = offset;
            Velocity = velocity;
            Alpha = alpha;
            Size = size;
            Age = age;
        }
    }
}
