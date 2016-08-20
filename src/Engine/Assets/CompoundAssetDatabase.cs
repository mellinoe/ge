using System;
using System.Collections.Generic;
using Veldrid.Assets;

namespace Engine.Assets
{
    public class CompoundAssetDatabase : AssetDatabase
    {
        private HashSet<AssetDatabase> _databases = new HashSet<AssetDatabase>();
        public void AddDatabase(AssetDatabase db)
        {
            if (!_databases.Add(db))
            {
                throw new InvalidOperationException("Cannot add database twice: " + db);
            }
        }

        public AssetID[] GetAssetsOfType(Type t)
        {
            List<AssetID> ids = new List<AssetID>();
            foreach (var db in _databases)
            {
                ids.AddRange(db.GetAssetsOfType(t));
            }

            return ids.ToArray();
        }

        public T LoadAsset<T>(AssetRef<T> assetRef)
        {
            return LoadAsset<T>(assetRef.ID);
        }

        public T LoadAsset<T>(AssetID assetID)
        {
            foreach (var db in _databases)
            {
                T asset;
                if (db.TryLoadAsset<T>(assetID, out asset))
                {
                    return asset;
                }
            }

            throw new InvalidOperationException("No asset with ID " + assetID + " was found in any asset database.");
        }

        public object LoadAsset(AssetID assetID)
        {
            foreach (var db in _databases)
            {
                object asset;
                if (db.TryLoadAsset<object>(assetID, out asset))
                {
                    return asset;
                }
            }

            throw new InvalidOperationException("No asset with ID " + assetID + " was found in any asset database.");
        }

        public bool TryLoadAsset<T>(AssetID assetID, out T asset)
        {
            foreach (var db in _databases)
            {
                if (db.TryLoadAsset<T>(assetID, out asset))
                {
                    return true;
                }
            }

            asset = default(T);
            return false;
        }
    }
}
