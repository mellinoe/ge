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
            GameObject.Transform.TransformChanged += SetViewMatrix;

            _gs = registry.GetSystem<GraphicsSystem>();
            _gs.Context.DataProviders.Add("ViewMatrixBuffer", _viewProvider);
            _gs.Context.DataProviders.Add("ProjectionMatrixBuffer", _projectionProvider);
        }

        public override void Removed(SystemRegistry registry)
        {
            _gs.Context.DataProviders.Remove("ViewMatrixBuffer");
            _gs.Context.DataProviders.Remove("ProjectionMatrixBuffer");
        }

        private void SetViewMatrix(Transform t)
        {
            _viewProvider.Data = Matrix4x4.CreateLookAt(t.Position, t.Position + t.Forward, t.Up);
        }

        private void SetProjectionMatrix()
        {
            _projectionProvider.Data = Matrix4x4.CreatePerspectiveFieldOfView(
                _fieldOfView,
                _gs.Context.Window.Width / _gs.Context.Window.Height,
                _nearPlane,
                _farPlane);
        }
    }
}
