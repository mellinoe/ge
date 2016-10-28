using Engine.Assets.Wire;
using System.Collections.Generic;
using System.IO;
using Veldrid.Assets;
using Wire;
using System;
using Engine.Assets;

namespace Engine.Editor
{
    public static class WireAssetDatabaseBuilder
    {
        private static readonly Type s_textAssetLoaderType = typeof(TextAssetLoader<>);

        public static void CreateWireAssetDatabase(LooseFileDatabase lfd, string targetPath)
        {
            SerializerOptions options = new SerializerOptions(serializerFactories: new[] { new WireComponentSerializerFactory() });
            Serializer serializer = new Serializer(options);
            List<WireDatabaseIndex.AssetInfo> infos = new List<WireDatabaseIndex.AssetInfo>();
            foreach (var id in lfd.GetAssetsOfType(typeof(object)))
            {
                object asset = lfd.LoadAsset(id);
                string copyDestination = GetAssetPath(targetPath, id);
                string copyDirectory = Path.GetDirectoryName(copyDestination);
                EditorUtility.EnsureDirectoryExists(copyDirectory);
                if (IsTextAsset(lfd, asset))
                {
                    using (var fs = OpenWriteStream(targetPath, id))
                    {
                        serializer.Serialize(asset, fs);
                    }
                }
                else
                {
                    File.Copy(lfd.GetAssetPath(id), copyDestination, overwrite:true);
                }

                WireDatabaseIndex.AssetInfo info = new WireDatabaseIndex.AssetInfo(id, asset.GetType());

                infos.Add(info);
            }

            WireDatabaseIndex index = new WireDatabaseIndex(infos.ToArray());
            using (var fs = OpenWriteStream(targetPath, "wiredb.index"))
            {
                serializer.Serialize(index, fs);
            }
        }

        private static bool IsTextAsset(LooseFileDatabase lfd, object asset)
        {
            Type t = asset.GetType();
            AssetLoader loader;
            if (!lfd.AssetLoaders.TryGetLoader(t, out loader))
            {
                return true;
            }

            var loaderType = loader.GetType();
            return (loaderType.IsConstructedGenericType && loaderType.GetGenericTypeDefinition() == s_textAssetLoaderType);
        }

        private static Stream OpenWriteStream(string rootPath, AssetID id)
        {
            string path = GetAssetPath(rootPath, id);
            return File.OpenWrite(path);
        }

        private static string GetAssetPath(string rootPath, AssetID id)
        {
            return Path.Combine(rootPath, id);
        }
    }
}
