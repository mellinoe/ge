using Engine.Graphics;
using System;
using System.Collections.Generic;
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

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _material;
        private int _indexCount;

        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public AxesRenderer(RenderContext rc)
        {
            _vb = rc.ResourceFactory.CreateVertexBuffer(6 * VertexPositionColor.SizeInBytes, false);
            _vb.SetVertexData(new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(0, 0, 0), RgbaFloat.Red),
                new VertexPositionColor(new Vector3(1, 0, 0), RgbaFloat.Red),
                new VertexPositionColor(new Vector3(0, 0, 0), RgbaFloat.Green),
                new VertexPositionColor(new Vector3(0, 1, 0), RgbaFloat.Green),
                new VertexPositionColor(new Vector3(0, 0, 0), RgbaFloat.Blue),
                new VertexPositionColor(new Vector3(0, 0, -1), RgbaFloat.Blue),
            }, new VertexDescriptor(VertexPositionColor.SizeInBytes, 2, 0, IntPtr.Zero));
            _ib = rc.ResourceFactory.CreateIndexBuffer(6 * 4, false);
            _ib.SetIndices(
                new int[] { 0, 1, 2, 3, 4, 5 },
                0,
                0);
            _indexCount = 6;
            _material = CreateMaterial(rc);
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
            return RenderOrderKey.Create(Vector3.Distance(viewPosition, Position), _material.GetHashCode());
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
            rc.DrawIndexedPrimitives(_indexCount, 0, PrimitiveTopology.LineList);
        }
    }
}
