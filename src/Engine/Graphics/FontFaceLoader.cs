using System.IO;
using SharpFont;
using Veldrid.Assets;

namespace Engine.Graphics
{
    public class FontFaceLoader : AssetLoader<FontFace>
    {
        public string FileExtension => "ttf";

        public FontFace Load(Stream s)
        {
            return new FontFace(s);
        }

        object AssetLoader.Load(Stream s)
        {
            return Load(s);
        }
    }
}