using Engine.Assets;
using Veldrid.Assets;

namespace Engine.Editor
{
    public class EditorEmbeddedAssets : EmbeddedAssetDatabase
    {
        public static readonly AssetID ArrowPointerID = "Internal:ArrowModel";

        public EditorEmbeddedAssets()
        {
            RegisterAsset(ArrowPointerID, ArrowPointerModel.MeshData);
        }
    }
}
