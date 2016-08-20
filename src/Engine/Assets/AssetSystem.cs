using Newtonsoft.Json;
using System;
using System.IO;
using Veldrid.Assets;

namespace Engine.Assets
{
    public class AssetSystem : GameSystem
    {
        private readonly AssetDatabase _ad;

        public AssetSystem()
        {
            _ad = CreateAssetDatabase();
        }

        protected virtual AssetDatabase CreateAssetDatabase()
        {
            var fileAssets = new LooseFileDatabase(Path.Combine(AppContext.BaseDirectory, "Assets"));
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