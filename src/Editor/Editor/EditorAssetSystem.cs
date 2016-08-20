using System;
using Engine.Assets;
using Veldrid.Assets;

namespace Engine.Editor
{
    public class EditorAssetSystem : AssetSystem
    {
        private LooseFileDatabase _projectAssetDatabase;
        public LooseFileDatabase ProjectDatabase => _projectAssetDatabase;

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
            return compoundDB;
        }
    }
}
