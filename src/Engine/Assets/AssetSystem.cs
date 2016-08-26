using Newtonsoft.Json;
using System;
using System.IO;
using Veldrid.Assets;

namespace Engine.Assets
{
    public class AssetSystem : GameSystem
    {
        private readonly AssetDatabase _ad;
        private string _assetRootPath;

        public AssetSystem(string assetRootPath)
        {
            _assetRootPath = assetRootPath;
            _ad = CreateAssetDatabase();
            LooseFileDatabase.AddExtensionTypeMapping(".scene", typeof(SceneAsset));
        }

        protected virtual AssetDatabase CreateAssetDatabase()
        {
            var fileAssets = new LooseFileDatabase(_assetRootPath);
            var embeddedAssets = new EngineEmbeddedAssets();
            var compoundDB = new CompoundAssetDatabase();
            compoundDB.AddDatabase(fileAssets);
            compoundDB.AddDatabase(embeddedAssets);
            return compoundDB;
        }

        public AssetDatabase Database => _ad;

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}