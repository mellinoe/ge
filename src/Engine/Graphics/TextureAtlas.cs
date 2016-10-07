using SharpFont;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class TextureAtlas : IGlyphAtlas, IDisposable
    {
        private readonly DeviceTexture2D _texture;
        private readonly ShaderTextureBinding _textureBinding;
        private readonly RenderContext _rc;
        private readonly DynamicDataProvider<FontAtlasInfo> _atlasInfo;
        private readonly int _size;

        public int Width => _size;
        public int Height => _size;
        public DeviceTexture2D Texture => _texture;
        public ShaderTextureBinding TextureBinding => _textureBinding;
        public ConstantBufferDataProvider AtlasInfo => _atlasInfo;

        public TextureAtlas(RenderContext rc, int size)
        {
            _size = size;
            _rc = rc;
            _texture = rc.ResourceFactory.CreateTexture(IntPtr.Zero, size, size, 1, PixelFormat.R8_UInt);
            _textureBinding = rc.ResourceFactory.CreateShaderTextureBinding(_texture);
            _atlasInfo = new DynamicDataProvider<FontAtlasInfo>(new FontAtlasInfo(size));
        }

        public void Dispose() => _texture.Dispose();

        public void Insert(int page, int x, int y, int width, int height, IntPtr data)
        {
            if (page > 0)
            {
                throw new NotImplementedException();
            }

            _texture.SetTextureData(x, y, width, height, data, width * height);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FontAtlasInfo : IEquatable<FontAtlasInfo>
    {
        public float Width;
        private readonly Vector3 __unused;

        public FontAtlasInfo(float width)
        {
            Width = width;
            __unused = new Vector3();
        }

        public bool Equals(FontAtlasInfo other)
        {
            return Width.Equals(other.Width);
        }
    }
}
