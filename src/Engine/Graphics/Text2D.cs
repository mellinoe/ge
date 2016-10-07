using Engine.Assets;
using SharpFont;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class Text2D : Component, RenderItem
    {
        private GraphicsSystem _gs;
        private AssetSystem _as;
        private TextBuffer _textBuffer;
        private TextureAtlas _textureAtlas;
        private TextAnalyzer _textAnalyzer;
        private DynamicDataProvider<Vector4> _textOffset = new DynamicDataProvider<Vector4>();

        private string _text;
        private bool _textChanged;
        private AssetRef<FontFace> _fontRef;
        private float _fontSize = 10;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    _textChanged = true;
                }
            }
        }

        public AssetRef<FontFace> Font
        {
            get { return _fontRef; }
            set { _fontRef = value; _textChanged = true; }
        }

        public float FontSize
        {
            get { return _fontSize; }
            set { _fontSize = value; _textChanged = true; }
        }

        public Vector2 ScreenPosition
        {
            get { return _textOffset.Data.XY(); }
            set { _textOffset.Data = new Vector4(value, 0, 0); }
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = registry.GetSystem<AssetSystem>();
            _textBuffer = new TextBuffer(_gs.Context);
            _textureAtlas = new TextureAtlas(_gs.Context, 2048);
            _textAnalyzer = new TextAnalyzer(_textureAtlas);
        }

        protected override void Removed(SystemRegistry registry)
        {
            _textBuffer.Dispose();
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

        public IEnumerable<string> GetStagesParticipated()
        {
            return CommonStages.Overlay;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (_fontRef != null && !_fontRef.ID.IsEmpty)
            {
                if (_textChanged)
                {
                    _textChanged = false;
                    _textBuffer.Clear();
                    _textAnalyzer.Clear();
                    FontFace font = _as.Database.LoadAsset(_fontRef);
                    _textBuffer.Append(
                        _textAnalyzer,
                        font,
                        _text,
                        FontSize * _gs.Context.Window.ScaleFactor.X,
                        _textureAtlas.Width,
                        new System.Drawing.RectangleF(0, 0, 1000, 1000));
                }

                _textBuffer.Render(_textureAtlas, _textOffset);
            }
        }
    }
}
