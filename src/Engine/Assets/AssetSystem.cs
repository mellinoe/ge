using Engine.Assets.Wire;
using Engine.Audio;
using Engine.Graphics;
using Newtonsoft.Json;
using SharpFont;
using System;
using System.IO;
using Veldrid.Assets;

namespace Engine.Assets
{
    public class AssetSystem : GameSystem
    {
        private readonly AssetDatabase _ad;
        private string _assetRootPath;

        public AssetSystem(string assetRootPath, SerializationBinder binder)
        {
            _assetRootPath = assetRootPath;
            _ad = CreateAssetDatabase(binder);
            LooseFileDatabase.AddExtensionTypeMapping(".scene", typeof(SceneAsset));
        }

        protected virtual AssetDatabase CreateAssetDatabase(SerializationBinder binder)
        {
            var compoundDB = new CompoundAssetDatabase();

            var embeddedAssets = new EngineEmbeddedAssets();
            compoundDB.AddDatabase(embeddedAssets);

            StreamLoaderSet streamLoaders = LooseFileDatabase.GetDefaultLoaderSet();
            streamLoaders.Add(typeof(WaveFile), new WaveFileLoader());
            streamLoaders.Add(typeof(FontFace), new FontFaceLoader());

            string wireIndexPath = Path.Combine(
                new DirectoryInfo(_assetRootPath).Parent.FullName, 
                "WireAssets", 
                "wiredb.index");
            if (File.Exists(wireIndexPath))
            {
                var wireAssets = new WireAssetDatabase(wireIndexPath, streamLoaders);
                compoundDB.AddDatabase(wireAssets);
            }
            else
            {
                var fileAssets = new LooseFileDatabase(_assetRootPath, streamLoaders);
                fileAssets.DefaultSerializer.Binder = binder;
                LooseFileDatabase.AddExtensionTypeMapping(".wav", typeof(WaveFile));
                LooseFileDatabase.AddExtensionTypeMapping(".ttf", typeof(FontFace));
                compoundDB.AddDatabase(fileAssets);
            }

            return compoundDB;
        }

        public AssetDatabase Database => _ad;

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}