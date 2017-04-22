using Engine.Assets;
using Engine.Physics;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class ColliderShapeRenderer : WireframeShapeRenderer
    {
        private GameObject _gameObject;
        private readonly AssetSystem _assetSystem;

        public GameObject GameObject { get { return _gameObject; } set { _gameObject = value; } }

        public ColliderShapeRenderer(AssetSystem assetSystem, RenderContext rc, RgbaFloat color) : base(rc, color)
        {
            _assetSystem = assetSystem;
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(BoundingBox.CreateFromVertices(_vertices)) == ContainmentType.Disjoint;
        }

        protected override void AddVerticesAndIndices()
        {
            foreach (var collider in GameObject.GetComponents<Collider>())
            {
                if (collider.Enabled)
                {
                    if (collider is BoxCollider)
                    {
                        AddBoxVertices((BoxCollider)collider);
                    }
                    else if (collider is SphereCollider)
                    {
                        AddSphereVertices((SphereCollider)collider);
                    }
                    else if (collider is MeshCollider)
                    {
                        AddMeshVertices((MeshCollider)collider);
                    }
                }
            }
        }

        private void AddBoxVertices(BoxCollider collider)
        {
            float width = collider.Width;
            float height = collider.Height;
            float depth = collider.Depth;
            Vector3 scale = new Vector3(width, height, depth);

            foreach (VertexPositionNormalTexture vertex in CubeModel.Vertices)
            {
                VertexPositionNormalTexture scaled = new VertexPositionNormalTexture(
                    Vector3.Transform(vertex.Position * scale, GameObject.Transform.GetWorldMatrix()),
                    Vector3.TransformNormal(vertex.Normal, GameObject.Transform.GetWorldMatrix()),
                    vertex.TextureCoordinates);
                _vertices.Add(scaled);
            }

            _indices.AddRange(CubeModel.Indices);
        }

        private void AddSphereVertices(SphereCollider collider)
        {
            float radius = collider.Radius;

            foreach (VertexPositionNormalTexture vertex in SphereModel.Vertices)
            {
                VertexPositionNormalTexture scaled = new VertexPositionNormalTexture(
                    Vector3.Transform(vertex.Position * radius, GameObject.Transform.GetWorldMatrix()),
                    Vector3.TransformNormal(vertex.Normal, GameObject.Transform.GetWorldMatrix()),
                    vertex.TextureCoordinates);
                _vertices.Add(scaled);
            }

            _indices.AddRange(SphereModel.Indices);
        }

        private void AddMeshVertices(MeshCollider collider)
        {
            MeshData meshData = collider.Mesh.Get(_assetSystem.Database);
            if (meshData != null)
            {
                Vector3[] positions = meshData.GetVertexPositions();
                ushort[] indices = meshData.GetIndices();

                foreach (Vector3 position in positions)
                {
                    _vertices.Add(new VertexPositionNormalTexture(
                        Vector3.Transform(position, GameObject.Transform.GetWorldMatrix()),
                        Vector3.Zero,
                        Vector2.Zero));
                }

                _indices.AddRange(indices);
            }
        }
    }
}
