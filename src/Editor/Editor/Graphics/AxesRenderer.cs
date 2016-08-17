using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Editor.Graphics
{
    public class AxesRenderer : BoundsRenderItem
    {
        private static readonly string[] s_stages = { "Standard" };

        private readonly DynamicDataProvider<Matrix4x4> _worldProvider = new DynamicDataProvider<Matrix4x4>();
        private readonly GraphicsSystem _gs;

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _material;
        private DepthStencilState _dss;
        private RasterizerState _rs;
        private int _lineIndicesCount;

        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public AxesRenderer(RenderContext rc, GraphicsSystem gs)
        {
            _gs = gs;
            _vb = rc.ResourceFactory.CreateVertexBuffer(6 * VertexPositionColor.SizeInBytes, false);

            const float planeLength = 0.2f;
            const float opacity = 0.66f;
            RgbaFloat red = new RgbaFloat(1, 0, 0, opacity);
            RgbaFloat green = new RgbaFloat(0, 1, 0, opacity);
            RgbaFloat blue = new RgbaFloat(0, 0, 1, opacity);
            _vb.SetVertexData(
                new VertexPositionColor[]
                {
                    new VertexPositionColor(new Vector3(0, 0, 0), red), // 0
                    new VertexPositionColor(new Vector3(1, 0, 0), red),
                    new VertexPositionColor(new Vector3(planeLength, 0, 0), red),
                    new VertexPositionColor(new Vector3(planeLength, planeLength, 0), red),
                    new VertexPositionColor(new Vector3(0, planeLength, 0), red),

                    new VertexPositionColor(new Vector3(0, 0, 0), green), // 5
                    new VertexPositionColor(new Vector3(0, 1, 0), green),
                    new VertexPositionColor(new Vector3(0, planeLength, 0), green),
                    new VertexPositionColor(new Vector3(0, planeLength, -planeLength), green),
                    new VertexPositionColor(new Vector3(0, 0, -planeLength), green),

                    new VertexPositionColor(new Vector3(0, 0, 0), blue), // 10
                    new VertexPositionColor(new Vector3(0, 0, -1), blue),
                    new VertexPositionColor(new Vector3(0, 0, -planeLength), blue),
                    new VertexPositionColor(new Vector3(planeLength, 0, -planeLength), blue),
                    new VertexPositionColor(new Vector3(planeLength, 0, 0), blue),
                },
                new VertexDescriptor(VertexPositionColor.SizeInBytes, 2, 0, IntPtr.Zero));
            _ib = rc.ResourceFactory.CreateIndexBuffer(6 * 4, false);
            _ib.SetIndices(
                new int[] { 0, 1, 5, 6, 10, 11, 0, 2, 3, 0, 3, 4, 5, 7, 8, 5, 8, 9, 10, 12, 13, 10, 13, 14 },
                0,
                0);
            _lineIndicesCount = 6;
            _material = CreateMaterial(rc);
            _dss = rc.ResourceFactory.CreateDepthStencilState(false, DepthComparison.Always);
            _rs = rc.ResourceFactory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
        }

        private Material CreateMaterial(RenderContext rc)
        {
            return rc.ResourceFactory.CreateMaterial(rc, "unlit-vertex", "unlit-frag",
                new MaterialVertexInput(
                    32,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Float4)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix"),
                    new MaterialGlobalInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, "ViewMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, 16)),
                MaterialTextureInputs.Empty);
        }

        public BoundingBox Bounds
        {
            get
            {
                Matrix4x4 m = GetWorldMatrix();
                return BoundingBox.Transform(new BoundingBox(-Vector3.One, Vector3.One), m);
            }
        }

        private Matrix4x4 GetWorldMatrix()
        {
            return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(Bounds) == ContainmentType.Disjoint;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public IEnumerable<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            _worldProvider.Data = GetWorldMatrix();
            rc.SetVertexBuffer(_vb);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            _material.ApplyPerObjectInput(_worldProvider);
            var previousDSS = rc.DepthStencilState;
            rc.DepthStencilState = _dss;
            var previousRS = rc.RasterizerState;
            rc.RasterizerState = _rs;
            rc.DrawIndexedPrimitives(_lineIndicesCount, 0, PrimitiveTopology.LineList);

            var previousBlendState = rc.BlendState;
            rc.BlendState = rc.AlphaBlend;
            var difference = _gs.MainCamera.Transform.Position - Position;
            if (difference.Y < 0)
            {
                if (difference.X > 0)
                {
                    DrawYZPlane(rc);
                    DrawXYPlane(rc);
                }
                else
                {
                    DrawXYPlane(rc);
                    DrawYZPlane(rc);
                }

                DrawXZPlane(rc);
            }
            else
            {
                DrawXZPlane(rc);

                if (difference.X > 0)
                {
                    DrawYZPlane(rc);
                    DrawXYPlane(rc);
                }
                else
                {
                    DrawXYPlane(rc);
                    DrawYZPlane(rc);
                }

            }

            rc.DepthStencilState = previousDSS;
            rc.RasterizerState = previousRS;
            rc.BlendState = previousBlendState;
        }

        private void DrawXYPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount);
        }

        private void DrawYZPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount + 6);
        }

        private void DrawXZPlane(RenderContext rc)
        {
            rc.DrawIndexedPrimitives(6, _lineIndicesCount + 12);
        }
    }
}
