using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Veldrid.Assets;

namespace Engine.Assets
{
    public class EmbeddedAssetDatabase : AssetDatabase
    {
        private static readonly Type s_lazyGenericType = typeof(Lazy<>);

        private readonly Dictionary<AssetID, object> _assets = new Dictionary<AssetID, object>();

        public void RegisterAsset<T>(AssetID id, T asset)
        {
            _assets.Add(id, asset);
        }

        public void RegisterAsset<T>(AssetID id, Lazy<T> asset)
        {
            _assets.Add(id, asset);
        }

        public override T LoadAsset<T>(AssetRef<T> assetRef, bool cache)
        {
            return LoadAsset<T>(assetRef.ID);
        }

        public override T LoadAsset<T>(AssetID assetID, bool cache)
        {
            return MaterializeAsset<T>(_assets[assetID]);
        }

        public override object LoadAsset(AssetID assetID, bool cache)
        {
            return MaterializeAsset<object>(_assets[assetID]);
        }

        public override AssetID[] GetAssetsOfType(Type t)
        {
            List<AssetID> ids = new List<AssetID>();
            foreach (var kvp in _assets)
            {
                Type assetType = MaterializeAssetType(kvp.Value);
                if (t.IsAssignableFrom(assetType))
                {
                    ids.Add(kvp.Key);
                }
            }

            return ids.ToArray();
        }

        public override bool TryLoadAsset<T>(AssetID id, bool cache, out T asset)
        {
            object assetAsObject;
            if (_assets.TryGetValue(id, out assetAsObject))
            {
                asset = MaterializeAsset<T>(assetAsObject);
                return true;
            }
            else
            {
                asset = default(T);
                return false;
            }
        }

        public override Boolean TryOpenAssetStream(AssetID assetID, out Stream stream)
        {
            stream = null;
            return false;
        }

        public override Stream OpenAssetStream(AssetID assetID)
        {
            throw new NotSupportedException("EmbeddedAssetDatabase does not support opening an asset Stream.");
        }

        private T MaterializeAsset<T>(object asset)
        {
            var valueType = asset.GetType();
            if (valueType.IsConstructedGenericType && valueType.GetGenericTypeDefinition() == s_lazyGenericType)
            {
                var itemType = valueType.GenericTypeArguments[0];
                if (itemType == typeof(T))
                {
                    return ((Lazy<T>)asset).Value;
                }
                if (!typeof(T).IsAssignableFrom(itemType))
                {
                    throw new InvalidOperationException("Asset type mismatch. Desired: " + typeof(T).Name + ", Actual: " + itemType.Name);
                }
                else
                {
                    var property = valueType.GetProperty("Value");
                    return (T)property.GetValue(asset);
                }
            }
            else
            {
                return (T)asset;
            }
        }

        private Type MaterializeAssetType(object value)
        {
            var valueType = value.GetType();
            if (valueType.IsConstructedGenericType && valueType.GetGenericTypeDefinition() == s_lazyGenericType)
            {
                return valueType.GenericTypeArguments[0];
            }
            else
            {
                return value.GetType();
            }
        }
    }
}
