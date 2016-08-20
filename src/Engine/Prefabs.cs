using Engine.Graphics;
using Engine.Physics;
using System;
using System.IO;
using System.Numerics;
using Veldrid.Graphics;

namespace Engine
{
    public static class Prefabs
    {
        public static GameObject CreateBin()
        {
            var shapes = new BoxShapeDescription[]
            {
                new BoxShapeDescription(new Vector3(3.0f, 0.5f, 3.0f), new Vector3(0f, 0f, 0f)), // Bottom
                new BoxShapeDescription(new Vector3(3.0f, 0.5f, 3.0f),
                    new Vector3(-1.75f, 1.75f, 0f), Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, (float)Math.PI / 2)),
                new BoxShapeDescription(new Vector3(3.0f, 0.5f, 3.0f),
                    new Vector3(1.75f, 1.75f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 2)),
                new BoxShapeDescription(new Vector3(3.0f, 0.5f, 3.0f),
                    new Vector3(0f, 1.75f, 1.75f), Quaternion.CreateFromAxisAngle(-Vector3.UnitX, (float)Math.PI / 2)),
                new BoxShapeDescription(new Vector3(3.0f, 0.5f, 3.0f),
                    new Vector3(0f, 1.75f, -1.75f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2))
            };

            GameObject bin = new GameObject("Bin");
            var csc = new CompoundShapeCollider(shapes, 40.0f);
            bin.AddComponent(csc);

            foreach (var shape in shapes)
            {
                var mc = new MeshRenderer(
                    new SimpleMeshDataProvider(CubeModel.Vertices, CubeModel.Indices),
                    Path.Combine("Textures", "Stone.png"));
                mc.RenderOffset = Matrix4x4.CreateScale(3.0f, 0.5f, 3.0f)
                    * Matrix4x4.CreateFromQuaternion(shape.Orientation)
                    * Matrix4x4.CreateTranslation(csc.EntityCenter + shape.Position)
                    ;
                bin.AddComponent(mc);
            }

            return bin;
        }
    }
}
