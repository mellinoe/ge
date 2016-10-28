using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Veldrid.Assets;
using Wire;

namespace Engine.Assets.Wire
{
    public class WireAssetDatabase : AssetDatabase
    {
        private readonly string _rootPath;
        private readonly Serializer _serializer;
        private readonly WireDatabaseIndex _index;
        private readonly StreamLoaderSet _assetLoaders;

        public WireAssetDatabase(string indexPath, StreamLoaderSet loaders)
        {
            _rootPath = Path.GetDirectoryName(indexPath);
            _assetLoaders = loaders;
            SerializerOptions options = new SerializerOptions(serializerFactories: new[] { new WireComponentSerializerFactory() });
            _serializer = new Serializer(options);
            using (var fs = File.OpenRead(indexPath))
            {
                _index = _serializer.Deserialize<WireDatabaseIndex>(fs);
            }
        }

        public override AssetID[] GetAssetsOfType(Type t)
        {
            return _index.Assets.Where(ai => t.IsAssignableFrom(ai.Type)).Select(ai => ai.ID).ToArray();
        }

        public override object LoadAsset(AssetID assetID, bool cache)
        {
            using (var stream = OpenStream(assetID))
            {
                AssetLoader loader;
                Type type = _index.GetAssetTypeByID(assetID);
                if (_assetLoaders.TryGetLoader(type, out loader))
                {
                    return loader.Load(stream);
                }
                else
                {
                    return _serializer.Deserialize(stream);
                }
            }
        }

        public override T LoadAsset<T>(AssetRef<T> assetRef, bool cache)
        {
            using (var stream = OpenStream(assetRef.ID))
            {
                AssetLoader loader;
                if (_assetLoaders.TryGetLoader(typeof(T), out loader))
                {
                    return (T)loader.Load(stream);
                }
                else
                {
                    return _serializer.Deserialize<T>(stream);
                }
            }
        }

        public override T LoadAsset<T>(AssetID assetID, bool cache)
        {
            using (var stream = OpenStream(assetID))
            {
                AssetLoader loader;
                if (_assetLoaders.TryGetLoader(typeof(T), out loader))
                {
                    return (T)loader.Load(stream);
                }
                else
                {
                    return _serializer.Deserialize<T>(stream);
                }
            }
        }

        public override bool TryLoadAsset<T>(AssetID assetID, bool cache, out T asset)
        {
            if (File.Exists(GetAssetPath(assetID)))
            {
                asset = LoadAsset<T>(assetID);
                return true;
            }

            asset = default(T);
            return false;
        }

        private string GetAssetPath(AssetID assetID)
        {
            return Path.Combine(_rootPath, assetID);
        }

        private Stream OpenStream(AssetID assetID)
        {
            return File.OpenRead(GetAssetPath(assetID));
        }
    }
}
