using System;
using System.Numerics;
using BEPUphysics.Entities;
using Veldrid.Graphics;
using Engine.Assets;
using BEPUphysics.Entities.Prefabs;
using System.Linq;
using Engine.Graphics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUutilities;

namespace Engine.Physics
{
    public class MeshCollider : Collider
    {
        private AssetSystem _as;

        private RefOrImmediate<MeshData> _mesh;
        public RefOrImmediate<MeshData> Mesh
        {
            get
            {
                return _mesh;
            }
            set
            {
                _mesh = value;
                if (Entity != null)
                {
                    MeshChanged();
                }
            }
        }

        public MeshCollider() : base(1.0f) { }

        public MeshCollider(float mass) : base(mass)
        {
        }

        private void MeshChanged()
        {
            SetEntity(CreateEntity());
        }

        protected override void PostAttached(SystemRegistry registry)
        {
            _as = registry.GetSystem<AssetSystem>();
            if (_mesh.GetRef() != null)
            {
                SetEntity(CreateEntity());
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            if (Entity == null && Mesh.GetRef() != null)
            {
                SetEntity(CreateEntity());
            }
        }

        protected override Entity CreateEntity()
        {
            MeshData meshData = Mesh.Get(_as.Database);
            Vector3[] positions = meshData.GetVertexPositions().Select(v => v * Transform.Scale).ToArray();
            int[] indices = meshData.GetIndices().Select(u16 => checked((int)u16)).ToArray();
            Vector3 center;
            MobileMeshShape mms = new MobileMeshShape(
                positions,
                indices,
                AffineTransform.Identity,
                MobileMeshSolidity.Solid,
                out center);

            foreach (var meshRenderer in GameObject.GetComponents<MeshRenderer>())
            {
                meshRenderer.RenderOffset = Matrix4x4.CreateTranslation(-center / Transform.Scale);
            }

            return new Entity(mms, Mass);
        }

        protected override void ScaleChanged(Vector3 scale)
        {
            if (Entity != null)
            {
                SetEntity(CreateEntity());
            }
        }
    }
}
