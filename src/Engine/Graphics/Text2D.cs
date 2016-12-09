using Engine.Assets;
using SharpFont;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;
using System;
using Newtonsoft.Json;

namespace Engine.Graphics
{
    public class Text2D : Component, RenderItem
    {
        private GraphicsSystem _gs;
        private AssetSystem _as;
        private DynamicDataProvider<Vector4> _textOffset = new DynamicDataProvider<Vector4>();
        private Vector2 _absoluteOffset;
        private Vector2 _relativeOffset;
        private TextAnchor _anchor = TextAnchor.CenterLeft;

        private TextBuffer _textBuffer;
        private TextAnalyzer _textAnalyzer;
        private TextureAtlas _textureAtlas;

        private CharBuffer _text = new CharBuffer(20);
        private bool _textChanged;
        private bool _fontChanged;
        private AssetRef<FontFace> _fontRef;
        private float _fontSize = 10;
        private FontFace _font;
        private bool _initialized;

        public void ClearText() => _text.Clear();

        public void Append(string s) => Append(s, 0, (uint)s.Length);
        public void Append(string s, uint start, uint count)
        {
            _text.Append(s, start, count);
            _textChanged = true;
        }

        public void Append(char[] chars, uint start, uint count)
        {
            _text.Append(chars, start, count);
            _textChanged = true;
        }

        public void Append(char value)
        {
            _text.Append(value);
            _textChanged = true;
        }

        public void Append(uint value) => Append(value, 0);
        public void Append(uint value, uint zeroPadDigits)
        {
            _text.Append(value, zeroPadDigits);
            _textChanged = true;
        }

        public AssetRef<FontFace> Font
        {
            get { return _fontRef; }
            set { _fontRef = value; _fontChanged = true; }
        }

        public float FontSize
        {
            get { return _fontSize; }
            set { _fontSize = value; _textChanged = true; }
        }

        public Vector2 ScreenAbsoluteOffset
        {
            get { return _absoluteOffset; }
            set { _absoluteOffset = value; UpdateTextOffset(); }
        }

        public Vector2 ScreenRelativePosition
        {
            get { return _relativeOffset; }
            set { _relativeOffset = value; UpdateTextOffset(); }
        }

        public TextAnchor Anchor
        {
            get { return _anchor; ; }
            set { _anchor = value; UpdateTextOffset(); }
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = registry.GetSystem<AssetSystem>();
            _gs.ExecuteOnMainThread(InitializeContextObjects);
        }

        private void InitializeContextObjects()
        {
            _textBuffer = new TextBuffer(_gs);
            _textureAtlas = new TextureAtlas(_gs.Context, 2048);
            _textAnalyzer = new TextAnalyzer(_textureAtlas);
            _gs.Context.WindowResized += OnWindowResized;
            _initialized = true;
        }

        protected override void Removed(SystemRegistry registry)
        {
            _textBuffer?.Dispose();
        }

        protected override void OnEnabled()
        {
            _gs.AddFreeRenderItem(this);
        }

        protected override void OnDisabled()
        {
            _gs.RemoveFreeRenderItem(this);
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return false;
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            return RenderOrderKey.Create(_textBuffer.GetMaterialID());
        }

        public IList<string> GetStagesParticipated()
        {
            return CommonStages.Overlay;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (!_initialized)
            {
                return;
            }

            if (_fontRef != null && !_fontRef.ID.IsEmpty)
            {
                if (_textChanged || _fontChanged)
                {
                    _textChanged = false;
                    RecreateTextBuffers();
                }

                _textBuffer.Render(_textureAtlas, _textOffset);
            }
        }

        private void RecreateTextBuffers()
        {
            _textBuffer.Clear();
            _textAnalyzer.Clear();

            if (_fontChanged)
            {
                _fontChanged = false;
                _font = _as.Database.LoadAsset(_fontRef);
            }

            _textBuffer.Append(
                _textAnalyzer,
                _font,
                _text,
                FontSize * _gs.Context.Window.ScaleFactor.X,
                _textureAtlas.Width,
                new System.Drawing.RectangleF(0, 0, 1000, 1000));

            UpdateTextOffset();
        }

        private void UpdateTextOffset()
        {
            if (_gs != null)
            {
                Vector2 anchorOffset = Vector2.Zero;
                {
                    switch (_anchor)
                    {
                        case TextAnchor.TopLeft:
                            anchorOffset = _textBuffer.Size * new Vector2(0f, 0f);
                            break;
                        case TextAnchor.TopCenter:
                            anchorOffset = _textBuffer.Size * new Vector2(-.5f, 0f);
                            break;
                        case TextAnchor.TopRight:
                            anchorOffset = _textBuffer.Size * new Vector2(-1f, 0f);
                            break;
                        case TextAnchor.CenterLeft:
                            anchorOffset = _textBuffer.Size * new Vector2(0f, 0.5f);
                            break;
                        case TextAnchor.Center:
                            anchorOffset = _textBuffer.Size * new Vector2(-0.5f, 0.5f);
                            break;
                        case TextAnchor.CenterRight:
                            anchorOffset = _textBuffer.Size * new Vector2(-1f, 0.5f);
                            break;
                        case TextAnchor.BottomLeft:
                            anchorOffset = _textBuffer.Size * new Vector2(0f, 1f);
                            break;
                        case TextAnchor.BottomCenter:
                            anchorOffset = _textBuffer.Size * new Vector2(-0.5f, 1f);
                            break;
                        case TextAnchor.BottomRight:
                            anchorOffset = _textBuffer.Size * new Vector2(-1f, 1f);
                            break;
                        default:
                            throw new InvalidOperationException("Invalid anchor type: " + _anchor);
                    }
                }

                Vector2 relativeOffsetAmount = new Vector2(_relativeOffset.X * _gs.Context.Window.Width, _relativeOffset.Y * _gs.Context.Window.Height);

                if (_textOffset != null)
                {
                    _textOffset.Data = new Vector4(anchorOffset + _absoluteOffset + relativeOffsetAmount, 0, 0);
                }
            }
        }

        private void OnWindowResized()
        {
            if (_relativeOffset != Vector2.Zero)
            {
                UpdateTextOffset();
            }
        }
    }

    public enum TextAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }
}
