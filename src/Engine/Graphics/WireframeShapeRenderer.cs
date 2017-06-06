using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public abstract class WireframeShapeRenderer : RenderItem, IDisposable
    {
        private readonly RawTextureDataArray<RgbaFloat> _textureData;

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _material;
        private DeviceTexture _texture;
        private ShaderTextureBinding _textureBinding;

        private static string[] s_stages = new string[] { "Standard" };
        private RasterizerState _wireframeState;

        protected List<VertexPositionNormalTexture> _vertices = new List<VertexPositionNormalTexture>();
        protected List<ushort> _indices = new List<ushort>();
        private DynamicDataProvider<Matrix4x4> _worldProvider;
        private DependantDataProvider<Matrix4x4> _inverseTransposeWorldProvider;
        private ConstantBufferDataProvider[] _perObjectProviders;

        public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

        public WireframeShapeRenderer(RenderContext rc, RgbaFloat color)
        {
            _textureData = new RawTextureDataArray<RgbaFloat>(new RgbaFloat[] { color }, 1, 1, RgbaFloat.SizeInBytes, PixelFormat.R32_G32_B32_A32_Float);

            _worldProvider = new DynamicDataProvider<Matrix4x4>(Matrix4x4.Identity);
            _inverseTransposeWorldProvider = new DependantDataProvider<Matrix4x4>(_worldProvider, Utilities.CalculateInverseTranspose);
            _perObjectProviders = new ConstantBufferDataProvider[] { _worldProvider, _inverseTransposeWorldProvider };

            InitializeContextObjects(rc);
        }

        public void ChangeRenderContext(RenderContext rc)
        {
            Dispose();
            InitializeContextObjects(rc);
        }

        private void InitializeContextObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(1024, true);
            _ib = factory.CreateIndexBuffer(1024, true, IndexFormat.UInt16);
            _material = CreateWireframeMaterial(rc);
            _texture = _textureData.CreateDeviceTexture(factory);
            _textureBinding = factory.CreateShaderTextureBinding(_texture);
            _wireframeState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Wireframe, true, true);
        }

        private Material CreateWireframeMaterial(RenderContext rc)
        {
            return rc.ResourceFactory.CreateMaterial(rc, "textured-vertex", "lit-frag",
                new MaterialVertexInput(
                    32,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, 16),
                    new MaterialPerObjectInputElement("InverseTransposeWorldMatrixBuffer", MaterialInputType.Matrix4x4, 16)),
                new MaterialTextureInputs(new ManualTextureInput("surfaceTexture")));
        }

        public abstract bool Cull(ref BoundingFrustum visibleFrustum);

        public void Dispose()
        {
            _vb?.Dispose();
            _ib?.Dispose();
            _material?.Dispose();
            _texture?.Dispose();
            _textureBinding?.Dispose();
            _wireframeState?.Dispose();
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return RenderOrderKey.Create(_material.GetHashCode());
        }

        public IList<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            UpdateBuffers(rc);

            var rasterState = rc.RasterizerState;
            rc.SetVertexBuffer(_vb);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInputs(_perObjectProviders);
            _material.UseTexture(0, _textureBinding);
            rc.RasterizerState = _wireframeState;
            rc.DrawIndexedPrimitives(_indices.Count, 0, Topology);
            rc.RasterizerState = rasterState;
        }

        private void UpdateBuffers(RenderContext rc)
        {
            var factory = rc.ResourceFactory;
            _vertices.Clear();
            _indices.Clear();
            AddVerticesAndIndices();

            if (_vertices.Count > 0)
            {
                _vb.Dispose();
                _ib.Dispose();

                _vb = factory.CreateVertexBuffer(_vertices.Count * VertexPositionNormalTexture.SizeInBytes, false);
                _vb.SetVertexData(
                    _vertices.ToArray(),
                    new VertexDescriptor(
                        VertexPositionNormalTexture.SizeInBytes,
                        VertexPositionNormalTexture.ElementCount,
                        0,
                        IntPtr.Zero));
                _ib = factory.CreateIndexBuffer(sizeof(int) * _indices.Count, false);
                _ib.SetIndices(_indices.ToArray(), IndexFormat.UInt16);
            }
        }

        protected abstract void AddVerticesAndIndices();
    }

    public class OctreeRenderer<T> : WireframeShapeRenderer
    {
        private Octree<T> _octree;

        public OctreeRenderer(Octree<T> octree, RenderContext rc) : base(rc, RgbaFloat.Red)
        {
            _octree = octree;
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(_octree.CurrentRoot.Bounds) == ContainmentType.Disjoint;
        }

        protected override void AddVerticesAndIndices()
        {
            AddVerticesAndIndices(_octree.CurrentRoot, _vertices, _indices);
        }

        private void AddVerticesAndIndices(OctreeNode<T> octree, List<VertexPositionNormalTexture> vertices, List<ushort> indices)
        {
            ushort baseIndex = (ushort)vertices.Count;
            var bounds = octree.Bounds;

            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z), Vector3.Zero, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z), Vector3.Zero, Vector2.Zero));

            indices.Add((ushort)(baseIndex + 0));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 0));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 3));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 0));
            indices.Add((ushort)(baseIndex + 3));
            indices.Add((ushort)(baseIndex + 0));

            indices.Add((ushort)(baseIndex + 4));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 4));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 0));
            indices.Add((ushort)(baseIndex + 1));
            indices.Add((ushort)(baseIndex + 4));
            indices.Add((ushort)(baseIndex + 0));
            indices.Add((ushort)(baseIndex + 4));

            indices.Add((ushort)(baseIndex + 7));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 7));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 4));
            indices.Add((ushort)(baseIndex + 5));
            indices.Add((ushort)(baseIndex + 7));
            indices.Add((ushort)(baseIndex + 4));
            indices.Add((ushort)(baseIndex + 7));

            indices.Add((ushort)(baseIndex + 3));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 3));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 2));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 7));
            indices.Add((ushort)(baseIndex + 6));
            indices.Add((ushort)(baseIndex + 3));
            indices.Add((ushort)(baseIndex + 7));
            indices.Add((ushort)(baseIndex + 3));

            foreach (var child in octree.Children)
            {
                AddVerticesAndIndices(child, vertices, indices);
            }
        }
    }

    public class FrustumWireframeRenderer : WireframeShapeRenderer
    {
        private BoundingFrustum _frustum;

        public FrustumWireframeRenderer(BoundingFrustum frustum, RenderContext rc)
            : base(rc, RgbaFloat.Cyan)
        {
            _frustum = frustum;
        }

        public BoundingFrustum Frustum
        {
            get { return _frustum; }
            set { _frustum = value; }
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(ref _frustum) == ContainmentType.Disjoint;
        }

        protected override void AddVerticesAndIndices()
        {
            int baseIndex = _vertices.Count;
            FrustumCorners corners = _frustum.GetCorners();

            _vertices.Add(new VertexPositionNormalTexture(corners.NearTopLeft, Vector3.One * 0.5f, Vector2.One * 0.5f));
            _vertices.Add(new VertexPositionNormalTexture(corners.NearTopRight, Vector3.One * 0.5f, Vector2.One * 0.5f));
            _vertices.Add(new VertexPositionNormalTexture(corners.NearBottomRight, Vector3.One * 0.5f, Vector2.One * 0.5f));
            _vertices.Add(new VertexPositionNormalTexture(corners.NearBottomLeft, Vector3.One * 0.5f, Vector2.One * 0.5f));

            _vertices.Add(new VertexPositionNormalTexture(corners.FarTopLeft, Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(corners.FarTopRight, Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(corners.FarBottomRight, Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(corners.FarBottomLeft, Vector3.Zero, Vector2.Zero));

            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 3));

            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 7));

            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 6));

            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 2));
        }
    }

    public class BoundingBoxWireframeRenderer : WireframeShapeRenderer
    {
        private BoundingBox _box;

        public BoundingBoxWireframeRenderer(BoundingBox box, RenderContext rc)
            : base(rc, RgbaFloat.Cyan)
        {
            _box = box;
        }

        public BoundingBox Box
        {
            get { return _box; }
            set { _box = value; }
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(_box) == ContainmentType.Disjoint;
        }

        protected override void AddVerticesAndIndices()
        {
            int baseIndex = _vertices.Count;

            var min = _box.Min;
            var max = _box.Max;

            _vertices.Add(new VertexPositionNormalTexture(new Vector3(min.X, min.Y, min.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(min.X, max.Y, min.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(max.X, max.Y, min.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(max.X, min.Y, min.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(min.X, min.Y, max.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(min.X, max.Y, max.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(max.X, max.Y, max.Z), Vector3.Zero, Vector2.Zero));
            _vertices.Add(new VertexPositionNormalTexture(new Vector3(max.X, min.Y, max.Z), Vector3.Zero, Vector2.Zero));

            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 3));

            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 7));

            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 4));
            _indices.Add((ushort)(baseIndex + 0));
            _indices.Add((ushort)(baseIndex + 1));
            _indices.Add((ushort)(baseIndex + 5));
            _indices.Add((ushort)(baseIndex + 1));

            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 6));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 7));
            _indices.Add((ushort)(baseIndex + 3));
        }
    }

    public class BoundsRenderItemWireframeRenderer : BoundingBoxWireframeRenderer, BoundsRenderItem
    {
        private BoundsRenderItem _bri;

        public BoundsRenderItemWireframeRenderer(BoundsRenderItem bri, RenderContext rc)
            : base(bri.Bounds, rc)
        {
            _bri = bri;
        }

        public BoundingBox Bounds
        {
            get
            {
                return _bri.Bounds;
            }
        }

        public bool RayCast(Ray ray, out float distance)
        {
            if (ray.Intersects(Bounds))
            {
                distance = Vector3.Distance(_bri.Bounds.GetCenter(), ray.Origin);
                return true;
            }
            else
            {
                distance = 0f;
                return false;
            }
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            if (ray.Intersects(Bounds))
            {
                float distance = Vector3.Distance(_bri.Bounds.GetCenter(), ray.Origin);
                distances.Add(distance);
                return 1;
            }

            return 0;
        }

        protected override void AddVerticesAndIndices()
        {
            Box = _bri.Bounds;
            base.AddVerticesAndIndices();
        }
    }

    public class ManualWireframeRenderer : WireframeShapeRenderer
    {
        public ManualWireframeRenderer(RenderContext rc, RgbaFloat color) : base(rc, color)
        {
        }

        public List<VertexPositionNormalTexture> Vertices { get; private set; } = new List<VertexPositionNormalTexture>();
        public List<ushort> Indices { get; private set; } = new List<ushort>();

        public void Clear()
        {
            Vertices.Clear();
            Indices.Clear();
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            var box = BoundingBox.CreateFromVertices(Vertices.ToArray());
            return visibleFrustum.Contains(box) == ContainmentType.Disjoint;
        }

        protected override void AddVerticesAndIndices()
        {
            _vertices.AddRange(Vertices);
            _indices.AddRange(Indices);
        }
    }
}
