using System;
using System.Linq;
using Veldrid.Assets;

namespace Engine.Assets.Wire
{
    public class WireDatabaseIndex
    {
        public AssetInfo[] Assets { get; }

        public WireDatabaseIndex(AssetInfo[] assets)
        {
            Assets = assets;
        }

        public Type GetAssetTypeByID(AssetID id)
        {
            AssetInfo info = Assets.SingleOrDefault(ai => ai.ID == id);
            if (info.ID.IsEmpty)
            {
                throw new InvalidOperationException("There was no asset with ID " + id + " in the Wire index");
            }

            return info.Type;
        }

        public struct AssetInfo
        {
            public AssetID ID { get; private set; }
            public string TypeName { get; private set; }
            public Type Type => Type.GetType(TypeName);

            public AssetInfo(AssetID id, Type t)
            {
                ID = id;
                TypeName = t.FullName;
            }
        }
    }
}
