using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public static class PlaneModel
    {
        public static readonly VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[]
        {
            new VertexPositionNormalTexture(new Vector3(-0.5f, 0, -0.5f),   Vector3.UnitY, new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(0.5f, 0, -0.5f),    Vector3.UnitY, new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(0.5f, 0, 0.5f),     Vector3.UnitY, new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-0.5f, 0, 0.5f),    Vector3.UnitY, new Vector2(0, 1))
        };

        public static readonly ushort[] Indices = new ushort[]
        {
            0, 1, 2,
            0, 2, 3
        };

        private static MeshData _meshData = new SimpleMeshDataProvider(Vertices, Indices);
        public static MeshData MeshData => _meshData;
    }

    public static class CubeModel
    {
        public static readonly VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[]
        {
            // Top
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,-.5f),     new Vector3(0,1,0),     new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,-.5f),      new Vector3(0,1,0),     new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,.5f),       new Vector3(0,1,0),     new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,.5f),      new Vector3(0,1,0),     new Vector2(0, 1)),
            // Bottom                                                             
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,.5f),     new Vector3(0,-1,0),     new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,.5f),      new Vector3(0,-1,0),     new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,-.5f),     new Vector3(0,-1,0),     new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector3(0,-1,0),     new Vector2(0, 1)),
            // Left                                                               
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,-.5f),     new Vector3(-1,0,0),    new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,.5f),      new Vector3(-1,0,0),    new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,.5f),     new Vector3(-1,0,0),    new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector3(-1,0,0),    new Vector2(0, 1)),
            // Right                                                              
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,.5f),       new Vector3(1,0,0),     new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,-.5f),      new Vector3(1,0,0),     new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,-.5f),     new Vector3(1,0,0),     new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,.5f),      new Vector3(1,0,0),     new Vector2(0, 1)),
            // Back                                                               
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,-.5f),      new Vector3(0,0,-1),    new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,-.5f),     new Vector3(0,0,-1),    new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector3(0,0,-1),    new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,-.5f),     new Vector3(0,0,-1),    new Vector2(0, 1)),
            // Front                                                              
            new VertexPositionNormalTexture(new Vector3(-.5f,.5f,.5f),      new Vector3(0,0,1),     new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,.5f,.5f),       new Vector3(0,0,1),     new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3(.5f,-.5f,.5f),      new Vector3(0,0,1),     new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-.5f,-.5f,.5f),     new Vector3(0,0,1),     new Vector2(0, 1)),
        };

        public static readonly ushort[] Indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };

        private static MeshData _meshData = new SimpleMeshDataProvider(Vertices, Indices);
        public static MeshData MeshData => _meshData;
    }

    public static class SphereModel
    {
        private static readonly ConstructedMeshInfo s_sphereMeshInfo = LoadShereMesh();
        public static MeshData MeshData => s_sphereMeshInfo;

        private static ConstructedMeshInfo LoadShereMesh()
        {
            Assembly assembly = typeof(SphereModel).GetTypeInfo().Assembly;
            using (var rs = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.Models.Sphere.obj"))
            {
                return new ObjParser().Parse(rs).GetFirstMesh();
            }
        }

        public static VertexPositionNormalTexture[] Vertices => s_sphereMeshInfo.Vertices;
        public static ushort[] Indices => s_sphereMeshInfo.Indices;
    }
}
