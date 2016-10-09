using System;
using System.Numerics;
using Veldrid.Graphics;
using Veldrid.Graphics.Pipeline;
using Veldrid.Platform;

namespace Engine.Graphics
{
    internal class UpscaleStage : PipelineStage
    {
        private Framebuffer _outputFramebuffer;
        private ShaderTextureBinding _textureBinding;
        private VertexBuffer _quadVB;
        private IndexBuffer _quadIB;
        private Material _quadMaterial;
        private ConstantDataProvider<Matrix4x4> _identityProvider = new ConstantDataProvider<Matrix4x4>(Matrix4x4.Identity);

        public DeviceTexture2D SourceTexture
        {
            get { return (DeviceTexture2D)_textureBinding?.BoundTexture; }
            set
            {
                _textureBinding?.Dispose();
                _textureBinding = RenderContext.ResourceFactory.CreateShaderTextureBinding(value);
            }
        }

        public UpscaleStage(RenderContext rc, string stageName, DeviceTexture2D sourceTexture, Framebuffer outputBuffer)
        {
            RenderContext = rc;
            Name = stageName;
            _outputFramebuffer = outputBuffer;

            ResourceFactory factory = rc.ResourceFactory;
            _quadVB = factory.CreateVertexBuffer(VertexPositionTexture.SizeInBytes * 4, false);
            _quadVB.SetVertexData(
                new VertexPositionTexture[]
                {
                    new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
                    new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1))
                }, new VertexDescriptor(VertexPositionTexture.SizeInBytes, 2, 0, IntPtr.Zero));
            _quadIB = factory.CreateIndexBuffer(sizeof(int) * 6, false);
            _quadIB.SetIndices(new int[] { 0, 1, 2, 0, 2, 3 });
            _quadMaterial = factory.CreateMaterial(rc, "simple-2d-vertex", "simple-2d-frag",
                new MaterialVertexInput(
                    20,
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new MaterialVertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)),
                new MaterialInputs<MaterialGlobalInputElement>(
                    new MaterialGlobalInputElement("WorldMatrixBuffer", MaterialInputType.Matrix4x4, _identityProvider),
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, _identityProvider)),
                MaterialInputs<MaterialPerObjectInputElement>.Empty,
                new MaterialTextureInputs(new ManualTextureInput("SurfaceTexture")));

            if (sourceTexture != null)
            {
                _textureBinding = factory.CreateShaderTextureBinding(sourceTexture);
            }
        }

        public bool Enabled { get; set; } = false;

        public string Name { get; }

        public RenderContext RenderContext { get; private set; }

        public void ChangeRenderContext(RenderContext rc)
        {
            RenderContext = rc;
        }

        public void ExecuteStage(VisibiltyManager visibilityManager, Vector3 viewPosition)
        {
            if (_outputFramebuffer == null)
            {
                Window window = RenderContext.Window;
                RenderContext.SetViewport(0, 0, window.Width, window.Height);
                RenderContext.SetDefaultFramebuffer();
            }
            else
            {
                RenderContext.SetViewport(0, 0, _outputFramebuffer.Width, _outputFramebuffer.Height);
                RenderContext.SetFramebuffer(_outputFramebuffer);
            }

            RenderContext.SetRasterizerState(RenderContext.DefaultRasterizerState);
            RenderContext.SetVertexBuffer(_quadVB);
            RenderContext.SetIndexBuffer(_quadIB);
            RenderContext.SetMaterial(_quadMaterial);
            _quadMaterial.UseTexture(0, _textureBinding);
            RenderContext.DrawIndexedPrimitives(6, 0);
        }
    }
}