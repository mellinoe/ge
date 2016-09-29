using System;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Graphics.Pipeline;

namespace Engine.Graphics
{
    public class ShadowMapStage : PipelineStage
    {
        private const int DepthMapWidth = 4096;
        private const int DepthMapHeight = 4096;

        private readonly RenderQueue _queue = new RenderQueue();
        private readonly string _contextBindingName = "ShadowMap";
        private readonly DynamicDataProvider<Matrix4x4> _lightViewProvider = new DynamicDataProvider<Matrix4x4>();
        private readonly DynamicDataProvider<Matrix4x4> _lightProjectionProvider = new DynamicDataProvider<Matrix4x4>();

        private Framebuffer _shadowMapFramebuffer;
        private DeviceTexture2D _depthTexture;

        public bool Enabled { get; set; } = true;

        public string Name => "ShadowMap";

        public RenderContext RenderContext { get; private set; }

        public DirectionalLight Light { get; set; }
        public Camera MainCamera { get; set; }

        public ShadowMapStage(RenderContext rc, string contextBindingName = "ShadowMap")
        {
            RenderContext = rc;
            _contextBindingName = contextBindingName;
            rc.RegisterGlobalDataProvider("LightViewMatrix", _lightViewProvider);
            rc.RegisterGlobalDataProvider("LightProjMatrix", _lightProjectionProvider);
        }

        public void ChangeRenderContext(RenderContext rc)
        {
            RenderContext = rc;
            InitializeContextObjects(rc);
        }

        private void InitializeContextObjects(RenderContext rc)
        {
            _depthTexture = rc.ResourceFactory.CreateDepthTexture(DepthMapWidth, DepthMapHeight, sizeof(ushort), PixelFormat.Alpha_UInt16);
            _shadowMapFramebuffer = rc.ResourceFactory.CreateFramebuffer();
            _shadowMapFramebuffer.DepthTexture = _depthTexture;
            rc.GetTextureContextBinding(_contextBindingName).Value = _depthTexture;
        }

        public void ExecuteStage(VisibiltyManager visibilityManager, Vector3 cameraPosition)
        {
            UpdateLightProjection();
            RenderContext.ClearScissorRectangle();
            RenderContext.SetFramebuffer(_shadowMapFramebuffer);
            RenderContext.ClearBuffer();
            RenderContext.SetViewport(0, 0, DepthMapWidth, DepthMapHeight);
            _queue.Clear();
            visibilityManager.CollectVisibleObjects(_queue, "ShadowMap", Vector3.Zero);
            _queue.Sort();

            foreach (RenderItem item in _queue)
            {
                item.Render(RenderContext, "ShadowMap");
            }
        }

        private void UpdateLightProjection()
        {
            if (MainCamera == null)
            {
                return;
            }
            if (Light == null)
            {
                _lightProjectionProvider.Data = Matrix4x4.Identity;
                _lightViewProvider.Data = Matrix4x4.Identity;
                return;
            }

            Vector3 cameraDir = MainCamera.Transform.Forward;
            Vector3 unitY = Vector3.UnitY;
            Vector3 cameraPosition = MainCamera.Transform.Position;
            FrustumCorners corners;
            FrustumHelpers.ComputePerspectiveFrustumCorners(
                ref cameraPosition,
                ref cameraDir,
                ref unitY,
                MainCamera.FieldOfViewRadians,
                MainCamera.NearPlaneDistance,
                MainCamera.FarPlaneDistance,
                (float)RenderContext.Window.Width / (float)RenderContext.Window.Height,
                out corners);

            // Approach used: http://alextardif.com/ShadowMapping.html

            Vector3 frustumCenter = Vector3.Zero;
            frustumCenter += corners.NearTopLeft;
            frustumCenter += corners.NearTopRight;
            frustumCenter += corners.NearBottomLeft;
            frustumCenter += corners.NearBottomRight;
            frustumCenter += corners.FarTopLeft;
            frustumCenter += corners.FarTopRight;
            frustumCenter += corners.FarBottomLeft;
            frustumCenter += corners.FarBottomRight;
            frustumCenter /= 8f;

            float radius = (corners.NearTopLeft - corners.FarBottomRight).Length() / 2.0f;
            float texelsPerUnit = (float)DepthMapWidth / (radius * 2.0f);

            Matrix4x4 scalar = Matrix4x4.CreateScale(texelsPerUnit, texelsPerUnit, texelsPerUnit);

            var _lightDirection = Light.Direction;
            Vector3 baseLookAt = -_lightDirection;

            Matrix4x4 lookat = Matrix4x4.CreateLookAt(Vector3.Zero, baseLookAt, Vector3.UnitY);
            lookat = scalar * lookat;
            Matrix4x4 lookatInv;
            Matrix4x4.Invert(lookat, out lookatInv);

            frustumCenter = Vector3.Transform(frustumCenter, lookat);
            frustumCenter.X = (int)frustumCenter.X;
            frustumCenter.Y = (int)frustumCenter.Y;
            frustumCenter = Vector3.Transform(frustumCenter, lookatInv);

            Vector3 lightPos = frustumCenter - (_lightDirection * radius * 2f);

            Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, frustumCenter, Vector3.UnitY);

            _lightProjectionProvider.Data = Matrix4x4.CreateOrthographicOffCenter(
                -radius, radius, -radius, radius, -radius * 4f, radius * 4f);
            _lightViewProvider.Data = lightView;
        }

        private void Dispose()
        {
            _shadowMapFramebuffer.Dispose();
        }
    }
}
