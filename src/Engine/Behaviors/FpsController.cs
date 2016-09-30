using Engine.Physics;
using ImGuiNET;
using Veldrid.Platform;
using System;
using System.Numerics;

namespace Engine.Behaviors
{
    public class FpsLookController : Behavior
    {
        private CharacterController _cc;
        private float _previousMouseX;
        private float _previousMouseY;
        private float _currentYaw;
        private float _currentPitch;

        private float _moveSpeed = 7.0f;
        private float _sprintFactor = 4f / 3f;

        private InputSystem _input;

        protected override void Start(SystemRegistry registry)
        {
            _input = registry.GetSystem<InputSystem>();
            _cc = GameObject.GetComponentInParentOrSelf<CharacterController>();
            _cc.Controller.ViewDirection = Transform.Forward;
        }

        public override void Update(float deltaSeconds)
        {
            HandleMouseMovement();
            HandleKeyboardMovement(deltaSeconds);
        }

        void HandleMouseMovement()
        {
            float newMouseX = _input.MousePosition.X;
            float newMouseY = _input.MousePosition.Y;

            float xDelta = newMouseX - _previousMouseX;
            float yDelta = newMouseY - _previousMouseY;

            if ((_input.GetMouseButton(MouseButton.Left) || _input.GetMouseButton(MouseButton.Right)) && !ImGui.IsMouseHoveringAnyWindow())
            {
                _currentYaw += -xDelta * 0.01f;
                _currentPitch += -yDelta * 0.01f;

                _currentPitch = MathUtil.Clamp(_currentPitch, ((float)-Math.PI / 2f) + .01f, ((float)Math.PI / 2f) - .01f);

                _cc.Controller.ViewDirection = Transform.Forward;
            }

            Transform.Rotation = Quaternion.CreateFromYawPitchRoll(_currentYaw, _currentPitch, 0f);

            _previousMouseX = newMouseX;
            _previousMouseY = newMouseY;
        }

        private void HandleKeyboardMovement(float deltaSeconds)
        {
            Vector3 movementDirection = new Vector3();
            if (_input.GetKey(Key.W))
            {
                movementDirection += Vector3.UnitZ;
            }
            if (_input.GetKey(Key.S))
            {
                movementDirection += -Vector3.UnitZ;
            }
            if (_input.GetKey(Key.A))
            {
                movementDirection += Vector3.UnitX;
            }
            if (_input.GetKey(Key.D))
            {
                movementDirection += -Vector3.UnitX;
            }
            if (movementDirection != Vector3.Zero)
            {
                Vector3 normalized = Vector3.Normalize(movementDirection);
                normalized.Y = 0f;
                Vector2 motionDirection = new Vector2(-normalized.X, normalized.Z);
                _cc.SetMotionDirection(motionDirection * MovementSpeed * deltaSeconds);
            }
            else
            {
                _cc.SetMotionDirection(Vector2.Zero);
            }

            float currentSpeed = _moveSpeed;
            if (_input.GetKey(Key.ShiftLeft))
            {
                currentSpeed *= _sprintFactor;
            }

            _cc.Controller.StandingSpeed = currentSpeed;

            if (_input.GetKeyDown(Key.Space))
            {
                JumpButtonPressed();
            }
        }

        private void JumpButtonPressed()
        {
            _cc.Controller.Jump();
        }

        public float MovementSpeed { get { return 5.0f; } }
    }
}
