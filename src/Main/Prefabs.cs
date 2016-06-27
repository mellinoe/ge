using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Ge.Graphics;
using Ge.Physics;
using System;
using System.IO;
using System.Numerics;
using Veldrid.Graphics;

namespace Ge
{
    public static class Prefabs
    {
        public static GameObject CreateBin()
        {
            var stoneTexture = new ImageProcessorTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "Stone.png"));

            var shapes = new CompoundShapeEntry[]
            {
                new CompoundShapeEntry(new BoxShape(3.0f, 0.5f, 3.0f), new Vector3(0f, 0.25f, 0f)), // Bottom
                new CompoundShapeEntry(new BoxShape(0.5f, 3.0f, 3.0f), new Vector3(-1.75f, 1.5f, 0f)), // Left
                new CompoundShapeEntry(new BoxShape(0.5f, 3.0f, 3.0f), new Vector3(1.75f, 1.5f, 0f)), // Right
                new CompoundShapeEntry(new BoxShape(3.0f, 3.0f, 0.5f), new Vector3(0f, 1.5f, 1.75f)), // Front
                new CompoundShapeEntry(new BoxShape(3.0f, 3.0f, 0.5f), new Vector3(0f, 1.5f, -1.75f)), // Back

            };

            GameObject bin = new GameObject("Bin");
            var csc = new CompoundShapeCollider(shapes, 4.0f);
            bin.AddComponent(csc);

            GameObject bottom = new GameObject("Bottom");
            bottom.Transform.Parent = bin.Transform;
            bottom.Transform.Scale = new Vector3(3.0f);
            bottom.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, stoneTexture) { DontCullBackFace = true, RenderOffset = csc.RenderOffset });

            GameObject front = new GameObject("Front");
            front.Transform.Parent = bin.Transform;
            front.Transform.LocalPosition = new Vector3(0, 1.5f, 1.5f);
            front.Transform.Scale = new Vector3(3.0f);
            front.Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2f);
            front.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, stoneTexture) { DontCullBackFace = true, RenderOffset = csc.RenderOffset });

            GameObject back = new GameObject("Back");
            back.Transform.Parent = bin.Transform;
            back.Transform.LocalPosition = new Vector3(0, 1.5f, -1.5f);
            back.Transform.Scale = new Vector3(3.0f);
            back.Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2f);
            back.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, stoneTexture) { DontCullBackFace = true, RenderOffset = csc.RenderOffset });

            GameObject left = new GameObject("Left");
            left.Transform.Parent = bin.Transform;
            left.Transform.LocalPosition = new Vector3(-1.5f, 1.5f, 0f);
            left.Transform.Scale = new Vector3(3.0f);
            left.Transform.Rotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, (float)Math.PI / 2f);
            left.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, stoneTexture) { DontCullBackFace = true, RenderOffset = csc.RenderOffset });

            GameObject right = new GameObject("Right");
            right.Transform.Parent = bin.Transform;
            right.Transform.LocalPosition = new Vector3(1.5f, 1.5f, 0f);
            right.Transform.Scale = new Vector3(3.0f);
            right.Transform.Rotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, -(float)Math.PI / 2f);
            right.AddComponent(new MeshRenderer(PlaneModel.Vertices, PlaneModel.Indices, stoneTexture) { DontCullBackFace = true, RenderOffset = csc.RenderOffset });

            return bin;
        }
    }
}
