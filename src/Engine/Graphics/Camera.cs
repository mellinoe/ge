using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class Camera : Component
    {
        private DynamicDataProvider<Matrix4x4> _viewProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<Matrix4x4> _projectionProvider = new DynamicDataProvider<Matrix4x4>();
        private DynamicDataProvider<CameraInfo> _cameraInfoProvider = new DynamicDataProvider<CameraInfo>();
        private GraphicsSystem _gs;

        private float _fov = 1.05f;
        private float _orthographicWidth = 35f;
        private float _nearPlaneDistance = 0.3f;
        private float _farPlaneDistance = 30f;
        private Vector3 _upDirection = Vector3.UnitY;
        private CameraProjectionType _projectionType = CameraProjectionType.Perspective;

        public CameraProjectionType ProjectionType { get { return _projectionType; } set { _projectionType = value; SetProjectionMatrix(); } }
        public float FieldOfViewRadians { get { return _fov; } set { _fov = value; SetProjectionMatrix(); } }
        public float NearPlaneDistance { get { return _nearPlaneDistance; } set { _nearPlaneDistance = value; SetProjectionMatrix(); } }
        public float FarPlaneDistance { get { return _farPlaneDistance; } set { _farPlaneDistance = value; SetProjectionMatrix(); } }
        public float OrthographicWidth { get { return _orthographicWidth; } set { _orthographicWidth = value; SetProjectionMatrix(); } }

        [JsonIgnore]
        public Vector3 GlobalUpDirection { get { return _upDirection; } set { _upDirection = value; if (Transform != null) { SetViewMatrix(Transform); } } }

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
            Debug.Assert(t != null);

            _viewProvider.Data = Matrix4x4.CreateLookAt(
                GameObject.Transform.Position,
                GameObject.Transform.Position + GameObject.Transform.Forward,
                Transform.Up);
            _cameraInfoProvider.Data = new CameraInfo(Transform.Position, Transform.Forward, NearPlaneDistance, FarPlaneDistance);
            UpdateViewFrustum();
        }

        private void SetProjectionMatrix()
        {
            if (_gs == null)
            {
                return;
            }

            Matrix4x4 projection;
            float aspectRatio = (float)_gs.Context.Window.Width / _gs.Context.Window.Height;
            switch (_projectionType)
            {
                case CameraProjectionType.Perspective:
                    projection = Matrix4x4.CreatePerspectiveFieldOfView(
                        FieldOfViewRadians,
                        aspectRatio,
                        NearPlaneDistance,
                        FarPlaneDistance);
                    break;
                case CameraProjectionType.Orthographic:
                    projection = Matrix4x4.CreateOrthographic(
                        _orthographicWidth,
                        _orthographicWidth / aspectRatio,
                        NearPlaneDistance,
                        FarPlaneDistance);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            _projectionProvider.Data = projection;
            _cameraInfoProvider.Data = new CameraInfo(Transform.Position, Transform.Forward, NearPlaneDistance, FarPlaneDistance);

            UpdateViewFrustum();
        }

        private void UpdateViewFrustum()
        {
            Veldrid.BoundingFrustum frustum = new Veldrid.BoundingFrustum(_viewProvider.Data * _projectionProvider.Data);
            _gs.SetViewFrustum(ref frustum);
        }
    }

    public enum CameraProjectionType
    {
        Perspective,
        Orthographic
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraInfo : IEquatable<CameraInfo>
    {
        public readonly Vector3 Position;
        public readonly float NearPlaneDistance;
        public readonly Vector3 LookDirection;
        public readonly float FarPlaneDistance;

        public CameraInfo(Vector3 position, Vector3 lookDirection, float near, float far)
        {
            Position = position;
            LookDirection = lookDirection;
            NearPlaneDistance = near;
            FarPlaneDistance = far;
        }

        public bool Equals(CameraInfo other)
        {
            return other.Position.Equals(Position) && other.LookDirection.Equals(LookDirection);
        }
    }
}
