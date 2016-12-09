using SharpFont;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    // A modified version of the TextBuffer class from SharpFont's "GPU demo".
    // Translated from Bgfx to Veldrid functions and made a few performance tweaks.

    public unsafe class TextBuffer : IDisposable
    {
        private IndexBuffer _ib;
        private VertexBuffer _vb;
        private Material _material;
        private int _filledIndexCount;
        private int _characterCount;
        private readonly RenderContext _rc;
        private ShaderConstantBindings _constantBindings;
        private DepthStencilState _dss;
        private DynamicDataProvider<Matrix4x4> _screenOrthoProjection = new DynamicDataProvider<Matrix4x4>();
        private ConstantBufferDataProvider[] _providers = new ConstantBufferDataProvider[3];
        private TextLayout _textLayout;
        private TextFormat _textFormat;
        private BEPUutilities.DataStructures.RawList<TextVertex> _textVertices = new BEPUutilities.DataStructures.RawList<TextVertex>(100);

        public Vector2 Size { get; private set; }

        public TextBuffer(GraphicsSystem gs)
        {
            _rc = gs.Context;
            _providers[0] = _screenOrthoProjection;
            _ib = _rc.ResourceFactory.CreateIndexBuffer(600, false);
            _material = CreateMaterial(_rc);
            _dss = _rc.ResourceFactory.CreateDepthStencilState(false, DepthComparison.LessEqual);

            _textLayout = new TextLayout();
            _textFormat = new TextFormat();
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, string text, float fontSize, int atlasWidth, RectangleF drawRect)
        {
            Append(analyzer, font, text, fontSize, atlasWidth, drawRect, RgbaByte.White);
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, string text, float fontSize, int atlasWidth, RectangleF drawRect, RgbaByte color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _textLayout.Stuff.Clear();
            _textFormat.Font = font;
            _textFormat.Size = fontSize;

            analyzer.AppendText(text, _textFormat);
            analyzer.PerformLayout(drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height, _textLayout);
            PerformLayoutAndUpdateDeviceBuffers(atlasWidth, color);
        }

        public uint GetMaterialID()
        {
            return _material != null ? (uint)_material.GetHashCode() : 0;
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, CharBuffer charBuffer, float fontSize, int atlasWidth, RectangleF drawRect)
        {
            Append(analyzer, font, charBuffer, fontSize, atlasWidth, drawRect, RgbaByte.White);
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, CharBuffer charBuffer, float fontSize, int atlasWidth, RectangleF drawRect, RgbaByte color)
        {
            if (charBuffer.Count == 0)
            {
                return;
            }

            _textLayout.Stuff.Clear();
            _textFormat.Font = font;
            _textFormat.Size = fontSize;

            analyzer.AppendText(charBuffer.Buffer, 0, (int)charBuffer.Count, _textFormat);
            analyzer.PerformLayout(drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height, _textLayout);
            PerformLayoutAndUpdateDeviceBuffers(atlasWidth, color);
        }

        private void PerformLayoutAndUpdateDeviceBuffers(int atlasWidth, RgbaByte color)
        {
            Vector2 min = new Vector2(float.MaxValue);
            Vector2 max = new Vector2(float.MinValue);
            _textVertices.Clear();
            foreach (var thing in _textLayout.Stuff)
            {
                var width = thing.Width;
                var height = thing.Height;
                var region = new Vector4(thing.SourceX, thing.SourceY, width, height) / atlasWidth;
                var origin = new Vector2(thing.DestX, thing.DestY);
                if (origin.X > -100000 && origin.Y > -10000)
                {
                    Vector2 localMax = origin + new Vector2(width, height);
                    min = Vector2.Min(min, origin);
                    max = Vector2.Max(max, localMax);
                }
                _textVertices.Add(new TextVertex(origin + new Vector2(0, height), new Vector2(region.X, region.Y + region.W), color));
                _textVertices.Add(new TextVertex(origin + new Vector2(width, height), new Vector2(region.X + region.Z, region.Y + region.W), color));
                _textVertices.Add(new TextVertex(origin + new Vector2(width, 0), new Vector2(region.X + region.Z, region.Y), color));
                _textVertices.Add(new TextVertex(origin, new Vector2(region.X, region.Y), color));
                _characterCount++;
            }

            Size = new Vector2(max.X - min.X, max.Y - min.Y);

            if (_vb == null)
            {
                _vb = _rc.ResourceFactory.CreateVertexBuffer(TextVertex.SizeInBytes * _textVertices.Count, false);
            }

            _vb.SetVertexData(_textVertices.GetArraySegment(), new VertexDescriptor(TextVertex.SizeInBytes, 3), 0);
            EnsureIndexCapacity(_characterCount);
        }

        public void Clear()
        {
            _characterCount = 0;
        }

        public void Render(TextureAtlas atlas, ConstantBufferDataProvider offsetProvider)
        {
            if (_vb != null) // If the VertexBuffer hasn't been created, then no text has been appended yet.
            {
                var previousDSS = _rc.DepthStencilState;
                _rc.DepthStencilState = _dss;
                var previousBlendState = _rc.BlendState;
                _rc.VertexBuffer = _vb;
                _rc.IndexBuffer = _ib;
                _rc.Material = _material;
                _screenOrthoProjection.Data = Matrix4x4.CreateOrthographicOffCenter(0, _rc.Window.Width, _rc.Window.Height, 0, -1f, 1f);
                _providers[1] = offsetProvider;
                _providers[2] = atlas.AtlasInfo;
                _material.ApplyPerObjectInputs(_providers);
                _rc.SetTexture(0, atlas.TextureBinding);
                _rc.BlendState = _rc.AlphaBlend;
                _rc.DrawIndexedPrimitives(_characterCount * 6, 0);
                _rc.BlendState = previousBlendState;
                _rc.DepthStencilState = previousDSS;
            }
        }

        private Material CreateMaterial(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            Shader vs = factory.CreateShader(ShaderType.Vertex, "text-vertex");
            Shader fs = factory.CreateShader(ShaderType.Fragment, "text-fragment");
            VertexInputLayout inputLayout = factory.CreateInputLayout(vs, new MaterialVertexInput(TextVertex.SizeInBytes,
                new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float2),
                new MaterialVertexInputElement("in_texCoords", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2),
                new MaterialVertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Byte4)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            _constantBindings = factory.CreateShaderConstantBindings(rc, shaderSet,
                MaterialInputs<MaterialGlobalInputElement>.Empty,
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, 64),
                    new MaterialPerObjectInputElement("TextOffsetBuffer", MaterialInputType.Float4, 16),
                    new MaterialPerObjectInputElement("AtlasInfoBuffer", MaterialInputType.Custom, 16)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet, new MaterialTextureInputs(new ManualTextureInput("FontAtlas")));
            Material material = new Material(rc, shaderSet, _constantBindings, textureSlots);

            return material;
        }

        private unsafe void EnsureIndexCapacity(int capacity)
        {
            int needed = capacity - _filledIndexCount;
            if (needed > 0)
            {
                ushort[] indices = new ushort[needed * 6];
                for (int i = 0, v = _filledIndexCount * 4; i < needed; i++, v += 4)
                {
                    indices[i * 6 + 0] = (ushort)(v + 0);
                    indices[i * 6 + 1] = (ushort)(v + 2);
                    indices[i * 6 + 2] = (ushort)(v + 1);
                    indices[i * 6 + 3] = (ushort)(v + 2);
                    indices[i * 6 + 4] = (ushort)(v + 0);
                    indices[i * 6 + 5] = (ushort)(v + 3);
                }

                _ib.SetIndices(indices, IndexFormat.UInt16, 0, _filledIndexCount * 6);
                _filledIndexCount = capacity;
            }
        }

        public void Dispose()
        {
            _vb?.Dispose();
            _ib?.Dispose();
            _material?.Dispose();
        }
    }

    internal struct TextVertex
    {
        public const byte SizeInBytes = 20;

        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaByte Color;

        public TextVertex(Vector2 position, Vector2 texcoords, RgbaByte color)
        {
            Position = position;
            TexCoords = texcoords;
            Color = color;
        }

        public TextVertex(Vector2 position, Vector2 texcoords, int color)
        {
            Position = position;
            TexCoords = texcoords;
            Color = new RgbaByte((uint)color);
        }
    }
}
