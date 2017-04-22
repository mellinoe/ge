using System.Reflection;
using Veldrid.Graphics;

namespace Engine.Editor
{
    internal static class ArrowPointerModel
    {
        private static readonly ConstructedMeshInfo s_arrowMeshInfo = LoadArrowPointerMesh();
        public static MeshData MeshData => s_arrowMeshInfo;

        private static ConstructedMeshInfo LoadArrowPointerMesh()
        {
            Assembly assembly = typeof(ArrowPointerModel).GetTypeInfo().Assembly;
            using (var rs = assembly.GetManifestResourceStream($"Engine.Editor.Assets.Models.ArrowPointerThin.obj"))
            {
                return new ObjParser().Parse(rs).GetFirstMesh();
            }
        }

        public static VertexPositionNormalTexture[] Vertices => s_arrowMeshInfo.Vertices;
        public static ushort[] Indices => s_arrowMeshInfo.Indices;
    }
}
