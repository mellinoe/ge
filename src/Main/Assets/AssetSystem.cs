using System;
using System.IO;
using Veldrid.Assets;

namespace Ge.Assets
{
    public class AssetSystem : GameSystem
    {
        private readonly LooseFileDatabase _ad;

        public AssetSystem()
        {
            _ad = new LooseFileDatabase(Path.Combine(AppContext.BaseDirectory, "Assets"));
        }

        public LooseFileDatabase Database => _ad;

        public override void Update(float deltaSeconds)
        {
        }
    }
}