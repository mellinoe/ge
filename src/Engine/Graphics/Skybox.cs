using Engine.Assets;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class Skybox : Component, RenderItem
    {
        private static ImageSharpTexture s_blankTexture = CreateBlankTexture();

        private GraphicsSystem _gs;
        private AssetSystem _as;

        private RefOrImmediate<ImageSharpTexture> _front;
        private RefOrImmediate<ImageSharpTexture> _back;
        private RefOrImmediate<ImageSharpTexture> _left;
        private RefOrImmediate<ImageSharpTexture> _right;
        private RefOrImmediate<ImageSharpTexture> _top;
        private RefOrImmediate<ImageSharpTexture> _bottom;

        private static ImageSharpTexture CreateBlankTexture()
        {
            return new ImageSharpTexture(new Image<Rgba32>(1, 1));
        }

        private ConstantBufferDataProvider _perObjectInput;

        // Context objects
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _material;
        private ShaderTextureBinding _cubemapBinding;
        private RasterizerState _rasterizerState;

        public Skybox()
        { }

        public Skybox(
            AssetRef<ImageSharpTexture> front, AssetRef<ImageSharpTexture> back, AssetRef<ImageSharpTexture> left,
            AssetRef<ImageSharpTexture> right, AssetRef<ImageSharpTexture> top, AssetRef<ImageSharpTexture> bottom)
        {
            _front = front;
            _back = back;
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = registry.GetSystem<AssetSystem>();
            var ad = _as.Database;
            RecreateCubemapTexture();
            _gs.ExecuteOnMainThread(() => InitializeContextObjects(ad, _gs.Context));
        }

        protected override void Removed(SystemRegistry registry)
        {
            _vb?.Dispose();
            _ib?.Dispose();
            _material?.Dispose();
            _rasterizerState?.Dispose();
            _cubemapBinding?.Dispose();
            _cubemapBinding?.BoundTexture?.Dispose();
        }

        protected override void OnEnabled()
        {
            _gs.AddFreeRenderItem(this);
        }

        protected override void OnDisabled()
        {
            _gs.RemoveFreeRenderItem(this);
        }

        public void InitializeContextObjects(AssetDatabase ad, RenderContext rc)
        {
            var factory = rc.ResourceFactory;

            _vb = factory.CreateVertexBuffer(s_vertices.Length * VertexPosition.SizeInBytes, false);
            _vb.SetVertexData(s_vertices, new VertexDescriptor(VertexPosition.SizeInBytes, 1, 0, IntPtr.Zero));

            _ib = factory.CreateIndexBuffer(s_indices.Length * sizeof(int), false);
            _ib.SetIndices(s_indices);

            _material = rc.ResourceFactory.CreateMaterial(rc, "skybox-vertex", "skybox-frag",
                new MaterialVertexInput(12,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("ViewMatrixBuffer", MaterialInputType.Matrix4x4, 16)),
                new MaterialTextureInputs(new ManualTextureInput("Skybox")));

            _perObjectInput = rc.GetNamedGlobalBufferProviderPair("ViewMatrix").DataProvider;

            RecreateCubemapTexture();

            _rasterizerState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, false, false);
            _depthStencilState = factory.CreateDepthStencilState(true, DepthComparison.LessEqual, false);
            _initialized = true;
        }

        private void RecreateCubemapTexture()
        {
            AssetDatabase ad = _as.Database;
            ResourceFactory factory = _gs.Context.ResourceFactory;
            _cubemapBinding?.BoundTexture.Dispose();
            _cubemapBinding?.Dispose();

            var front = !_front.HasValue ? _front.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxFrontID);
            var back = !_back.HasValue ? _back.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBackID);
            var left = !_left.HasValue ? _left.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxLeftID);
            var right = !_right.HasValue ? _right.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxRightID);
            var top = !_top.HasValue ? _top.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxTopID);
            var bottom = !_bottom.HasValue ? _bottom.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBottomID);

            using (var frontPin = front.Pixels.Pin())
            using (var backPin = back.Pixels.Pin())
            using (var leftPin = left.Pixels.Pin())
            using (var rightPin = right.Pixels.Pin())
            using (var topPin = top.Pixels.Pin())
            using (var bottomPin = bottom.Pixels.Pin())
            {
                var cubemapTexture = factory.CreateCubemapTexture(
                    frontPin.Ptr,
                    backPin.Ptr,
                    leftPin.Ptr,
                    rightPin.Ptr,
                    topPin.Ptr,
                    bottomPin.Ptr,
                    front.Width,
                    front.Height,
                    front.PixelSizeInBytes,
                    front.Format);
                _cubemapBinding = factory.CreateShaderTextureBinding(cubemapTexture);
            }
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            // Render the skybox last.
            return new RenderOrderKey(ulong.MaxValue - 1);
        }

        public IList<string> GetStagesParticipated()
        {
            return s_stages;
        }

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (!_initialized)
            {
                return;
            }
            rc.SetVertexBuffer(_vb);
            rc.SetIndexBuffer(_ib);
            rc.SetMaterial(_material);
            RasterizerState previousRasterState = rc.RasterizerState;
            DepthStencilState previousDepthStencilState = rc.DepthStencilState;
            rc.SetRasterizerState(_rasterizerState);
            rc.SetDepthStencilState(_depthStencilState);
            _material.UseTexture(0, _cubemapBinding);
            _material.ApplyPerObjectInput(_perObjectInput);
            rc.DrawIndexedPrimitives(s_indices.Length, 0);
            rc.SetRasterizerState(previousRasterState);
            rc.SetDepthStencilState(previousDepthStencilState);
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return false;
        }

        public RefOrImmediate<ImageSharpTexture> Front
        {
            get { return _front; }
            set
            {
                _front = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        public RefOrImmediate<ImageSharpTexture> Back
        {
            get { return _back; }
            set
            {
                _back = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        public RefOrImmediate<ImageSharpTexture> Left
        {
            get { return _left; }
            set
            {
                _left = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        public RefOrImmediate<ImageSharpTexture> Right
        {
            get { return _right; }
            set
            {
                _right = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        public RefOrImmediate<ImageSharpTexture> Bottom
        {
            get { return _bottom; }
            set
            {
                _bottom = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        public RefOrImmediate<ImageSharpTexture> Top
        {
            get { return _top; }
            set
            {
                _top = value;
                if (_cubemapBinding != null)
                {
                    RecreateCubemapTexture();
                }
            }
        }

        private static readonly VertexPosition[] s_vertices = new VertexPosition[]
        {
            // Top
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            // Bottom
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Left
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Right
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            // Back
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            // Front
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        };

        private static readonly int[] s_indices = new int[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };

        private static readonly string[] s_stages = { "Standard" };

        private DepthStencilState _depthStencilState;
        private bool _initialized;
    }
}