using ImGuiNET;
using System.Numerics;
using Veldrid.Platform;

namespace Engine.Behaviors
{
    public class FreeFlyMovement : Behavior
    {
        private InputSystem _input { get; set; }

        private float _previousMouseX;
        private float _previousMouseY;
        private float _currentYaw = 0.3f;
        private float _currentPitch = 0.3f;

        private float _speed = 5f;

        private float _turboMultiplier = 3f;
        private bool _draggingOffWindow;

        protected override void Start(SystemRegistry registry)
        {
            _input = registry.GetSystem<InputSystem>();
        }

        public override void Update(float deltaSeconds)
        {
            Vector3 moveDirection = Vector3.Zero;

            if (_input.GetKey(Key.W))
            {
                moveDirection += GameObject.Transform.Forward;
            }
            if (_input.GetKey(Key.S))
            {
                moveDirection -= GameObject.Transform.Forward;
            }
            if (_input.GetKey(Key.A))
            {
                moveDirection -= GameObject.Transform.Right;
            }
            if (_input.GetKey(Key.D))
            {
                moveDirection += GameObject.Transform.Right;
            }
            if (_input.GetKey(Key.E))
            {
                moveDirection += GameObject.Transform.Up;
            }
            if (_input.GetKey(Key.Q))
            {
                moveDirection -= GameObject.Transform.Up;
            }

            if (moveDirection != Vector3.Zero)
            {
                float totalSpeed = _speed * (_input.GetKey(Key.ShiftLeft) ? _turboMultiplier : 1.0f);
                GameObject.Transform.Position += Vector3.Normalize(moveDirection) * totalSpeed * deltaSeconds;
            }

            HandleMouseMovement();
        }


        void HandleMouseMovement()
        {
            float newMouseX = _input.MousePosition.X;
            float newMouseY = _input.MousePosition.Y;

            float xDelta = newMouseX - _previousMouseX;
            float yDelta = newMouseY - _previousMouseY;

            if (!_draggingOffWindow && ((_input.GetMouseButtonDown(MouseButton.Left) || _input.GetMouseButtonDown(MouseButton.Right)) && !ImGui.IsMouseHoveringAnyWindow()))
            {
                _draggingOffWindow = true;
            }

            if (_draggingOffWindow)
            {
                if (!(_input.GetMouseButton(MouseButton.Left) || _input.GetMouseButton(MouseButton.Right)))
                {
                    _draggingOffWindow = false;
                }
                else
                {
                    _currentYaw += -xDelta * 0.01f;
                    _currentPitch += -yDelta * 0.01f;

                    GameObject.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(_currentYaw, _currentPitch, 0f);
                }
            }

            _previousMouseX = newMouseX;
            _previousMouseY = newMouseY;
        }
    }
}
