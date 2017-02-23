using Veldrid.Graphics;
using Veldrid.Assets;
using Engine.Graphics;
using System.IO;
using System.Numerics;
using Engine.Physics;

namespace Engine.Editor.Graphics
{
    public static class ObjMatImporter
    {
        public static void GenerateGameObjectsFromFile(AssetDatabase ad, AssetID objID, AssetID matID, string mtlRoot)
        {
            ObjFile objFile = ad.LoadAsset<ObjFile>(objID);
            MtlFile mtlFile;
            using (var mtlStream = ad.OpenAssetStream(matID))
            {
                var parser = new MtlParser();
                mtlFile = parser.Parse(mtlStream);
            }

            foreach (var meshGroup in objFile.MeshGroups)
            {
                ConstructedMeshInfo meshInfo = objFile.GetMesh(meshGroup);
                MaterialDefinition mtlDef = mtlFile.Definitions[meshGroup.Material];
                
                GameObject go = new GameObject(meshGroup.Name);
                string textureAsset;
                if (string.IsNullOrEmpty(mtlDef.DiffuseTexture))
                {
                    textureAsset = Assets.EngineEmbeddedAssets.PinkTextureID;
                }
                else
                {
                    textureAsset = Path.Combine(mtlRoot, mtlDef.DiffuseTexture);
                }
                MeshRenderer mr = new MeshRenderer(meshInfo, textureAsset);
                go.AddComponent(mr);
                go.Transform.Scale = new Vector3(.1f);
            }
        }
    }
}