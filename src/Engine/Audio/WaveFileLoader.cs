using System.IO;
using Veldrid.Assets;

namespace Engine.Audio
{
    public class WaveFileLoader : AssetLoader<WaveFile>
    {
        public string FileExtension => "wav";

        public WaveFile Load(Stream s)
        {
            return new WaveFile(s);
        }

        object AssetLoader.Load(Stream s)
        {
            return Load(s);
        }
    }
}
