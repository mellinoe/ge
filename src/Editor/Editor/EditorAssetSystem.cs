using System;
using Engine.Assets;
using Newtonsoft.Json;
using Veldrid.Assets;

namespace Engine.Editor
{
    public class EditorAssetSystem : AssetSystem
    {
        private LooseFileDatabase _projectAssetDatabase;
        private EditorSerializationBinder _binder;

        public LooseFileDatabase ProjectDatabase => _projectAssetDatabase;

        public EditorAssetSystem(string assetRootPath) : base(assetRootPath)
        {
        }

        public EditorSerializationBinder Binder => _binder;

        public string ProjectAssetRootPath
        {
            get { return _projectAssetDatabase.RootPath; }
            set { _projectAssetDatabase.RootPath = value; }
        }

        protected override AssetDatabase CreateAssetDatabase()
        {
            var compoundDB = new CompoundAssetDatabase();
            compoundDB.AddDatabase(new EngineEmbeddedAssets());
            compoundDB.AddDatabase(new EditorEmbeddedAssets());
            _projectAssetDatabase = new LooseFileDatabase("Assets");
            compoundDB.AddDatabase(_projectAssetDatabase);
            _binder = new EditorSerializationBinder();
            _projectAssetDatabase.DefaultSerializer.Binder = _binder;
            return compoundDB;
        }
    }
}
