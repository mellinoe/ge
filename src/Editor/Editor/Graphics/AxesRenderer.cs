using BEPUutilities.DataStructures;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;

namespace Engine.Editor.Graphics
{
    public class AxesRenderer : BoundsRenderItem
    {
        private static readonly string[] s_stages = { "Standard" };
        private const float PlaneLength = 0.1f;

        private readonly DynamicDataProvider<Matrix4x4> _worldProvider = new DynamicDataProvider<Matrix4x4>();
        private readonly GraphicsSystem _gs;

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _material;
        private DepthStencilState _dss;
        private RasterizerState _rs;
        private int _lineIndicesCount;
        private readonly RawList<VertexPositionColor> _vertices = new RawList<VertexPositionColor>(30);

        private readonly VertexBuffer _pointerVB;
        private readonly IndexBuffer _pointerIB;
        private readonly int _pointerIndexCount;
        private readonly Material _pointerMaterial;
        private readonly DeviceTexture _redTexture;
        private readonly DeviceTexture _greenTexture;
        private readonly DeviceTexture _blueTexture;
        private readonly ShaderTextureBinding _redBinding;
        private readonly ShaderTextureBinding _greenBinding;
        private readonly ShaderTextureBinding _blueBinding;

        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public AxesRenderer(RenderContext rc, GraphicsSystem gs)
        {
            _gs = gs;
            _vb = rc.ResourceFactory.CreateVertexBuffer(6 * VertexPositionColor.SizeInBytes, false);

            const float opacity = 0.56f;
            RgbaFloat red = new RgbaFloat(1, 0, 0, opacity);
            RgbaFloat green = new RgbaFloat(0, 1, 0, opacity);
            RgbaFloat blue = new RgbaFloat(0, 0, 1, opacity);
            SetPlaneVertices(
                new Vector3(PlaneLength, 0, PlaneLength),
                new Vector3(PlaneLength, PlaneLength, 0),
                new Vector3(0, PlaneLength, PlaneLength));
            _ib = rc.ResourceFactory.CreateIndexBuffer(6 * 4, false);
            _ib.SetIndices(
                new int[]
                {
                    0, 1, 2, 3, 4, 5, // Lines
                    // Planes
                    6, 7, 8, 6, 8, 9,
                    10, 11, 12, 10, 12, 13,
                    14, 15, 16, 14, 16, 17,

                    // Solid plane borders
                    18, 19, 19, 20, 20, 21, 21, 18,
                    22, 23, 23, 24, 24, 25, 25, 22,
                    26, 27, 27, 28, 28, 29, 29, 26
                },
                0,
                0);
            _lineIndicesCount = 6;
            _material = CreateMaterial(rc);

            _pointerVB = ArrowPointerModel.MeshData.CreateVertexBuffer(rc.ResourceFactory);
            _pointerIB = ArrowPointerModel.MeshData.CreateIndexBuffer(rc.ResourceFactory, out _pointerIndexCount);
            _pointerMaterial = CreatePointerMaterial(rc);
            _redTexture = RawTextureDataArray<RgbaFloat>.FromSingleColor(RgbaFloat.Red).CreateDeviceTexture(rc.ResourceFactory);
            _greenTexture = RawTextureDataArray<RgbaFloat>.FromSingleColor(RgbaFloat.Green).CreateDeviceTexture(rc.ResourceFactory);
            _blueTexture = RawTextureDataArray<RgbaFloat>.FromSingleColor(RgbaFloat.Blue).CreateDeviceTexture(rc.ResourceFactory);
            _redBinding = rc.ResourceFactory.CreateShaderTextureBinding(_redTexture);
            _greenBinding = rc.ResourceFactory.CreateShaderTextureBinding(_greenTexture);
            _blueBinding = rc.ResourceFactory.CreateShaderTextureBinding(_blueTexture);

            _dss = rc.ResourceFactory.CreateDepthStencilState(false, DepthComparison.Always);
            _rs = rc.ResourceFactory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
        }

        private Material CreateMaterial(RenderContext rc)
        {
            return rc.ResourceFactory.CreateMaterial(rc, "unlit-vertex", "unlit-frag",
                new MaterialVertexInput(
                    28,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Float4)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, 16)),
                MaterialTextureInputs.Empty);
        }

        private Material CreatePointerMaterial(RenderContext rc)
        {
            return rc.ResourceFactory.CreateMaterial(rc, "arrow-pointer-vertex", "arrow-pointer-frag",
                new MaterialVertexInput(
                    VertexPositionNormalTexture.SizeInBytes,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, 16)),
                new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture")));
        }

        public BoundingBox Bounds
        {
            get
            {
                Matrix4x4 m = GetWorldMatrix();
                return BoundingBox.Transform(new BoundingBox(-Vector3.One, Vector3.One), m);
            }
        }

        private Matrix4x4 GetWorldMatrix(Vector3 offset = new Vector3())
        {
            return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position + offset);
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(Bounds) == ContainmentType.Disjoint;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public IList<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            DepthStencilState previousDSS = rc.DepthStencilState;
            rc.DepthStencilState = _dss;
            RasterizerState previousRS = rc.RasterizerState;
            rc.RasterizerState = _rs;
            BlendState previousBlendState = rc.BlendState;
            rc.BlendState = rc.AlphaBlend;

            // Draw Pointers
            rc.SetMaterial(_pointerMaterial);
            Matrix4x4 pointerScale = Matrix4x4.CreateScale(0.1f);

            _worldProvider.Data = pointerScale * GetWorldMatrix();
            _pointerMaterial.ApplyPerObjectInput(_worldProvider);
            _pointerMaterial.UseTexture(0, _greenBinding);
            rc.SetVertexBuffer(_pointerVB);
            rc.SetIndexBuffer(_pointerIB);
            rc.DrawIndexedPrimitives(_pointerIndexCount, 0);

            _worldProvider.Data = pointerScale * Matrix4x4.CreateRotationX(-(float)Math.PI / 2f) * GetWorldMatrix();
            _pointerMaterial.ApplyPerObjectInput(_worldProvider);
            _pointerMaterial.UseTexture(0, _blueBinding);
            rc.DrawIndexedPrimitives(_pointerIndexCount, 0);

            _worldProvider.Data = pointerScale * Matrix4x4.CreateRotationZ(-(float)Math.PI / 2f) * GetWorldMatrix();
            _pointerMaterial.ApplyPerObjectInput(_worldProvider);
            _pointerMaterial.UseTexture(0, _redBinding);
            rc.DrawIndexedPrimitives(_pointerIndexCount, 0);

            _worldProvider.Data = GetWorldMatrix();
            rc.SetVertexBuffer(_vb);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInput(_worldProvider);

            // Draw Planes

            Vector3 difference = Vector3.Transform(_gs.MainCamera.Transform.Position - Position, Quaternion.Inverse(Rotation));

            Vector3 xzOffset = (Math.Sign(difference.X) * Vector3.UnitX
                    + Math.Sign(difference.Z) * Vector3.UnitZ);

            Vector3 xyOffset = (Math.Sign(difference.X) * Vector3.UnitX
                + Math.Sign(difference.Y) * Vector3.UnitY);

            Vector3 yzOffset = (Math.Sign(difference.Y) * Vector3.UnitY
                + Math.Sign(difference.Z) * Vector3.UnitZ);

            SetPlaneVertices(xzOffset * PlaneLength, xyOffset * PlaneLength, yzOffset * PlaneLength);

            DrawXZPlane(rc);
            DrawXYPlane(rc);
            DrawYZPlane(rc);

            rc.RasterizerState = previousRS;
            rc.DepthStencilState = previousDSS;
            rc.BlendState = previousBlendState;
        }

        private void SetRenderOffset(Vector3 offset)
        {
            _worldProvider.Data = GetWorldMatrix(offset);
            _material.ApplyPerObjectInput(_worldProvider);
        }

        private void SetPlaneVertices(Vector3 xzOffset, Vector3 xyOffset, Vector3 yzOffset)
        {
            const float opacity = 0.66f;
            RgbaFloat red = new RgbaFloat(1, 0, 0, opacity);
            RgbaFloat green = new RgbaFloat(0, 1, 0, opacity);
            RgbaFloat blue = new RgbaFloat(0, 0, 1, opacity);

            RgbaFloat solidRed = new RgbaFloat(1, 0, 0, 1);
            RgbaFloat solidGreen = new RgbaFloat(0, 1, 0, 1);
            RgbaFloat solidBlue = new RgbaFloat(0, 0, 1, 1);

            _vertices.Clear();
            // Axes
            _vertices.Add(new VertexPositionColor(new Vector3(0, 0, 0), red)); // 0
            _vertices.Add(new VertexPositionColor(new Vector3(1, 0, 0), red));
            _vertices.Add(new VertexPositionColor(new Vector3(0, 0, 0), green)); // 2
            _vertices.Add(new VertexPositionColor(new Vector3(0, 1, 0), green));
            _vertices.Add(new VertexPositionColor(new Vector3(0, 0, 0), blue)); // 4
            _vertices.Add(new VertexPositionColor(new Vector3(0, 0, -1), blue));

            // Planes
            // XY
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(-PlaneLength, PlaneLength, 0), blue)); // 6
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(-PlaneLength, -PlaneLength, 0), blue));
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(PlaneLength, -PlaneLength, 0), blue));
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(PlaneLength, PlaneLength, 0), blue));

            // YZ
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, -PlaneLength, PlaneLength), red)); // 10
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, -PlaneLength, -PlaneLength), red));
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, PlaneLength, -PlaneLength), red));
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, PlaneLength, PlaneLength), red));

            // XZ
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(-PlaneLength, 0, PlaneLength), green)); // 14
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(-PlaneLength, 0, -PlaneLength), green));
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(PlaneLength, 0, -PlaneLength), green));
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(PlaneLength, 0, PlaneLength), green));

            // Planes - Solid Borders
            // XY
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(-PlaneLength, PlaneLength, 0), solidBlue)); // 6
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(-PlaneLength, -PlaneLength, 0), solidBlue));
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(PlaneLength, -PlaneLength, 0), solidBlue));
            _vertices.Add(new VertexPositionColor(xyOffset + new Vector3(PlaneLength, PlaneLength, 0), solidBlue));

            // YZ
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, -PlaneLength, PlaneLength), solidRed)); // 10
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, -PlaneLength, -PlaneLength), solidRed));
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, PlaneLength, -PlaneLength), solidRed));
            _vertices.Add(new VertexPositionColor(yzOffset + new Vector3(0, PlaneLength, PlaneLength), solidRed));

            // XZ
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(-PlaneLength, 0, PlaneLength), solidGreen)); // 14
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(-PlaneLength, 0, -PlaneLength), solidGreen));
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(PlaneLength, 0, -PlaneLength), solidGreen));
            _vertices.Add(new VertexPositionColor(xzOffset + new Vector3(PlaneLength, 0, PlaneLength), solidGreen));

            _vb.SetVertexData(
                new ArraySegment<VertexPositionColor>(_vertices.Elements, 0, _vertices.Count),
                new VertexDescriptor(VertexPositionColor.SizeInBytes, 2, 0, IntPtr.Zero),
                0);
        }

        private void DrawXYPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount);
            rc.DrawIndexedPrimitives(8, _lineIndicesCount + 18, PrimitiveTopology.LineList);
        }

        private void DrawYZPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount + 6);
            rc.DrawIndexedPrimitives(8, _lineIndicesCount + 26, PrimitiveTopology.LineList);
        }

        private void DrawXZPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount + 12);
            rc.DrawIndexedPrimitives(8, _lineIndicesCount + 34, PrimitiveTopology.LineList);
        }

        public bool RayCast(Ray ray, out float distance)
        {
            if (ray.Intersects(Bounds))
            {
                distance = Vector3.Distance(Position, ray.Origin);
                return true;
            }
            else
            {
                distance = 0;
                return false;
            }
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            if (ray.Intersects(Bounds))
            {
                float distance = Vector3.Distance(Bounds.GetCenter(), ray.Origin);
                distances.Add(distance);
                return 1;
            }

            return 0;
        }
    }
}
