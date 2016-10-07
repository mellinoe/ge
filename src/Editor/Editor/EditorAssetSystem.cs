using System;
using Engine.Assets;
using Newtonsoft.Json;
using Veldrid.Assets;
using Engine.Audio;
using Engine.Graphics;
using SharpFont;

namespace Engine.Editor
{
    public class EditorAssetSystem : AssetSystem
    {
        private LooseFileDatabase _projectAssetDatabase;

        public LooseFileDatabase ProjectDatabase => _projectAssetDatabase;

        public EditorAssetSystem(string assetRootPath, SerializationBinder binder) : base(assetRootPath, binder)
        {
        }

        public string ProjectAssetRootPath
        {
            get { return _projectAssetDatabase.RootPath; }
            set { _projectAssetDatabase.RootPath = value; }
        }

        protected override AssetDatabase CreateAssetDatabase(SerializationBinder binder)
        {
            var compoundDB = new CompoundAssetDatabase();
            compoundDB.AddDatabase(new EngineEmbeddedAssets());
            compoundDB.AddDatabase(new EditorEmbeddedAssets());
            _projectAssetDatabase = new LooseFileDatabase("Assets");
            _projectAssetDatabase.DefaultSerializer.Binder = binder;
            _projectAssetDatabase.RegisterTypeLoader(typeof(WaveFile), new WaveFileLoader());
            LooseFileDatabase.AddExtensionTypeMapping(".wav", typeof(WaveFile));
            _projectAssetDatabase.RegisterTypeLoader(typeof(FontFace), new FontFaceLoader());
            LooseFileDatabase.AddExtensionTypeMapping(".ttf", typeof(FontFace));

            compoundDB.AddDatabase(_projectAssetDatabase);
            return compoundDB;
        }
    }
}
