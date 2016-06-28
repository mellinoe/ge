using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Ge.Graphics;
using Ge.Physics;
using System;
using System.IO;
using System.Numerics;
using Veldrid.Graphics;
using BEPUutilities;

namespace Ge
{
    public static class Prefabs
    {
        public static GameObject CreateBin()
        {
            var stoneTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Stone.png"));

            var shapes = new CompoundShapeEntry[]
            {
                new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f), new Vector3(0f, 0f, 0f)), // Bottom
                new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f),
                    new RigidTransform(new Vector3(-1.75f, 1.75f, 0f), Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, (float)Math.PI / 2))),
                new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f),
                    new RigidTransform(new Vector3(1.75f, 1.75f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 2))),
                    new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f),
                    new RigidTransform(new Vector3(0f, 1.75f, 1.75f), Quaternion.CreateFromAxisAngle(-Vector3.UnitX, (float)Math.PI / 2))),
                new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f),
                    new RigidTransform(new Vector3(0f, 1.75f, -1.75f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2))),

            };

            GameObject bin = new GameObject("Bin");
            var csc = new CompoundShapeCollider(shapes, 4.0f);
            bin.AddComponent(csc);
            
            foreach (var shape in shapes)
            {
                var mc = new MeshRenderer(CubeModel.Vertices, CubeModel.Indices, stoneTexture);
                mc.RenderOffset = Matrix4x4.CreateScale(3.0f, 0.5f, 3.0f)
                    * Matrix4x4.CreateFromQuaternion(shape.LocalTransform.Orientation)
                    * Matrix4x4.CreateTranslation(csc.EntityCenter + shape.LocalTransform.Position)
                    ;
                bin.AddComponent(mc);
            }

            return bin;
        }
    }
}
