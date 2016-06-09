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
            SetViewMatrix(GameObject.Transform);

            _gs = registry.GetSystem<GraphicsSystem>();
            _gs.Context.DataProviders.Add("ViewMatrix", _viewProvider);
            _gs.Context.DataProviders.Add("ProjectionMatrix", _projectionProvider);

            _gs.Context.WindowResized += SetProjectionMatrix;
            SetProjectionMatrix();
        }

        public override void Removed(SystemRegistry registry)
        {
            _gs.Context.DataProviders.Remove("ViewMatrix");
            _gs.Context.DataProviders.Remove("ProjectionMatrix");
        }

        private void SetViewMatrix(Transform t)
        {
            _viewProvider.Data = Matrix4x4.CreateLookAt(
                GameObject.Transform.Position,
                GameObject.Transform.Position + GameObject.Transform.Forward,
                GameObject.Transform.Up);
        }

        private void SetProjectionMatrix()
        {
            _projectionProvider.Data = Matrix4x4.CreatePerspectiveFieldOfView(
                _fieldOfView,
                (float)_gs.Context.Window.Width / _gs.Context.Window.Height,
                _nearPlane,
                _farPlane);
        }
    }
}
