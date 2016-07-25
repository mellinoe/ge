using BEPUutilities;
using System.Numerics;
using Veldrid.Graphics;

namespace Ge.Graphics
{
    public class Camera : Component
    {
        private DynamicDataProvider<Matrix4x4> _viewProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<Matrix4x4> _projectionProvider = new DynamicDataProvider<Matrix4x4>();
        private GraphicsSystem _gs;

        private float _fieldOfView = 1.05f;
        private float _nearPlane = 0.1f;
        private float _farPlane = 1000f;

        public override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();

            GameObject.Transform.TransformChanged += SetViewMatrix;
            SetViewMatrix(GameObject.Transform);

            _gs.Context.RegisterGlobalDataProvider("ViewMatrix", _viewProvider);
            _gs.Context.RegisterGlobalDataProvider("ProjectionMatrix", _projectionProvider);
            _gs.SetMainCamera(this);

            _gs.Context.WindowResized += SetProjectionMatrix;
            SetProjectionMatrix();
        }

        public override void Removed(SystemRegistry registry)
        {
        }

        public Ray GetRayFromScreenPoint(float screenX, float screenY)
        {
            var window = _gs.Context.Window;

            // Normalized Device Coordinates
            float x = (2.0f * screenX) / window.Width - 1.0f;
            float y = 1.0f - (2.0f * screenY) / window.Height;
            float z = 1.0f;
            Vector3 deviceCoords = new Vector3(x, y, z);

            // Clip Coordinates
            Vector4 clipCoords = new Vector4(deviceCoords.X, deviceCoords.Y, -1.0f, 1.0f);

            // View Coordinates
            Matrix4x4 invProj;
            Matrix4x4.Invert(_projectionProvider.Data, out invProj);
            Vector4 viewCoords = Vector4.Transform(clipCoords, invProj);
            viewCoords.Z = -1.0f;
            viewCoords.W = 0.0f;

            Matrix4x4 invView;
            Matrix4x4.Invert(_viewProvider.Data, out invView);
            Vector3 worldCoords = Vector4.Transform(viewCoords, invView).XYZ();
            worldCoords = Vector3.Normalize(worldCoords);

            return new Ray(GameObject.Transform.Position, worldCoords);
        }

        private void SetViewMatrix(Transform t)
        {
            _viewProvider.Data = Matrix4x4.CreateLookAt(
                GameObject.Transform.Position,
                GameObject.Transform.Position + GameObject.Transform.Forward,
                GameObject.Transform.Up);

            UpdateViewFrustum();
        }

        private void SetProjectionMatrix()
        {
            _projectionProvider.Data = Matrix4x4.CreatePerspectiveFieldOfView(
                _fieldOfView,
                (float)_gs.Context.Window.Width / _gs.Context.Window.Height,
                _nearPlane,
                _farPlane);

            UpdateViewFrustum();
        }

        private void UpdateViewFrustum()
        {
            Veldrid.BoundingFrustum frustum = new Veldrid.BoundingFrustum(_viewProvider.Data * _projectionProvider.Data);
            _gs.SetViewFrustum(ref frustum);
        }
    }
}
