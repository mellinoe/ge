using Newtonsoft.Json;
using System;
using System.IO;

namespace Engine.Editor
{
    public class InMemoryAsset<T>
    {
        private byte[] _data;

        public InMemoryAsset(Stream stream)
        {
            _data = new byte[stream.Length];
            var ms = new MemoryStream(_data);
            stream.CopyTo(ms);
        }

        public InMemoryAsset()
        {
            _data = Array.Empty<byte>();
        }

        public T GetAsset(JsonSerializer serializer)
        {
            using (var ms = new MemoryStream(_data))
            using (var reader = new StreamReader(ms))
            using (var jtr = new JsonTextReader(reader))
            {
                return serializer.Deserialize<T>(jtr);
            }
        }

        public void UpdateAsset(JsonSerializer serializer, T asset)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms))
                using (var jtw = new JsonTextWriter(writer))
                {
                    serializer.Serialize(jtw, asset);
                }

                _data = ms.ToArray();
            }
        }

        public void UpdateAsset(JsonSerializer serializer, object asset)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms))
                using (var jtw = new JsonTextWriter(writer))
                {
                    serializer.Serialize(jtw, asset);
                }

                _data = ms.ToArray();
            }
        }
    }
}
