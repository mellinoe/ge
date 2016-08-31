using System.Numerics;
using Veldrid;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class Camera : Component
    {
        private DynamicDataProvider<Matrix4x4> _viewProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<Matrix4x4> _projectionProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<Vector4> _cameraInfoProvider = new DynamicDataProvider<Vector4>();
        private GraphicsSystem _gs;

        private float _fov = 1.05f;
        private float _nearPlaneDistance = 0.3f;
        private float _farPlaneDistance = 30f;
        private Vector3 _upDirection = Vector3.UnitY;

        public float FieldOfViewRadians { get { return _fov; } set { _fov = value; SetProjectionMatrix(); } }
        public float NearPlaneDistance { get { return _nearPlaneDistance; } set { _nearPlaneDistance = value; SetProjectionMatrix(); } }
        public float FarPlaneDistance { get { return _farPlaneDistance; } set { _farPlaneDistance = value; SetProjectionMatrix(); } }
        public Vector3 UpDirection { get { return _upDirection; } set { _upDirection = value; if (Transform != null) { SetViewMatrix(Transform); } } }

        public ConstantBufferDataProvider ViewProvider => _viewProvider;
        public ConstantBufferDataProvider ProjectionProvider => _projectionProvider;
        public ConstantBufferDataProvider CameraInfoProvider => _cameraInfoProvider;

        protected override void Attached(SystemRegistry registry)
        {
            _gs = registry.GetSystem<GraphicsSystem>();
        }

        protected override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            GameObject.Transform.TransformChanged += SetViewMatrix;
            _gs.Context.WindowResized += SetProjectionMatrix;
            _gs.SetMainCamera(this);

            SetViewMatrix(GameObject.Transform);
            SetProjectionMatrix();
            UpdateViewFrustum();
        }

        protected override void OnDisabled()
        {
            GameObject.Transform.TransformChanged -= SetViewMatrix;
            _gs.Context.WindowResized -= SetProjectionMatrix;
        }

        public Ray GetRayFromScreenPoint(float screenX, float screenY)
        {
            var window = _gs.Context.Window;

            // Normalized Device Coordinates Top-Left (-1, 1) to Bottom-Right (1, -1)
            float x = (2.0f * screenX) / (window.Width / window.ScaleFactor.X) - 1.0f;
            float y = 1.0f - (2.0f * screenY) / (window.Height / window.ScaleFactor.Y);
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
                _upDirection);

            _cameraInfoProvider.Data = new Vector4(Transform.Position, 1f);

            UpdateViewFrustum();
        }

        private void SetProjectionMatrix()
        {
            if (_gs == null)
            {
                return;
            }

            _projectionProvider.Data = Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfViewRadians,
                (float)_gs.Context.Window.Width / _gs.Context.Window.Height,
                NearPlaneDistance,
                FarPlaneDistance);

            UpdateViewFrustum();
        }

        private void UpdateViewFrustum()
        {
            Veldrid.BoundingFrustum frustum = new Veldrid.BoundingFrustum(_viewProvider.Data * _projectionProvider.Data);
            _gs.SetViewFrustum(ref frustum);
        }
    }
}
