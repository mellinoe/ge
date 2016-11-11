using System;
using System.Collections.Generic;
using System.IO;
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

        public override AssetID[] GetAssetsOfType(Type t)
        {
            List<AssetID> ids = new List<AssetID>();
            foreach (var db in _databases)
            {
                ids.AddRange(db.GetAssetsOfType(t));
            }

            return ids.ToArray();
        }

        public override T LoadAsset<T>(AssetRef<T> assetRef, bool cache)
        {
            return LoadAsset<T>(assetRef.ID, cache);
        }

        public override T LoadAsset<T>(AssetID assetID, bool cache)
        {
            foreach (var db in _databases)
            {
                T asset;
                if (db.TryLoadAsset<T>(assetID, cache, out asset))
                {
                    return asset;
                }
            }

            throw new InvalidOperationException("No asset with ID " + assetID + " was found in any asset database.");
        }

        public override object LoadAsset(AssetID assetID, bool cache)
        {
            foreach (var db in _databases)
            {
                object asset;
                if (db.TryLoadAsset(assetID, cache, out asset))
                {
                    return asset;
                }
            }

            throw new InvalidOperationException("No asset with ID " + assetID + " was found in any asset database.");
        }

        public override bool TryLoadAsset<T>(AssetID assetID, bool cache, out T asset)
        {
            foreach (var db in _databases)
            {
                if (db.TryLoadAsset<T>(assetID, cache, out asset))
                {
                    return true;
                }
            }

            asset = default(T);
            return false;
        }

        public override Stream OpenAssetStream(AssetID assetID)
        {
            foreach (var db in _databases)
            {
                Stream stream;
                if (db.TryOpenAssetStream(assetID, out stream))
                {
                    return stream;
                }
            }

            throw new InvalidOperationException("No asset with ID " + assetID + " was found in any asset database.");
        }

        public override Boolean TryOpenAssetStream(AssetID assetID, out Stream stream)
        {
            foreach (var db in _databases)
            {
                if (db.TryOpenAssetStream(assetID, out stream))
                {
                    return true;
                }
            }

            stream = null;
            return false;
        }
    }
}
