using System;
using System.Numerics;
using Engine.Physics;
using ImGuiNET;

namespace Engine.Behaviors
{
    public class CameraBob : Behavior
    {
        private float _stillRestTime = 2f/3f; // Seconds to return to origin when stopped.
        private CharacterController _cc;

        private float _displacementMax = 0.08f;
        private float _currentPhase = 0.0f; // 0 -> Pi / 2
        private float _currentDirection = 1; // 1 or -1

        private float _bobPeriod = 2.5f / 3f; // Seconds

        private Vector3 _origin;

        protected override void Start(SystemRegistry registry)
        {
            _cc = GameObject.GetComponentInParentOrSelf<CharacterController>();
            _origin = Transform.LocalPosition;
        }

        public override void Update(float deltaSeconds)
        {
            bool onGround = _cc.Controller.SupportFinder.HasSupport;
            float currentSpeed = _cc.Controller.Body.LinearVelocity.Length();
            float bobRate = currentSpeed / 7.0f;
            if (bobRate < 0.05f || !onGround)
            {
                float lerpTarget = _currentPhase > Math.PI / 2 ? (float)Math.PI : 0f;
                _currentPhase = MathUtil.Lerp(_currentPhase, 0f, (float)Math.PI * deltaSeconds / _stillRestTime);
                if (_currentPhase < (0.001f * (float)Math.PI * 2))
                {
                    _currentPhase = 0f;
                }
            }
            else
            {
                _currentPhase += (bobRate * deltaSeconds / _bobPeriod) * (float)Math.PI * 2;
                if (_currentPhase > (float)Math.PI)
                {
                    _currentPhase -= (float)Math.PI;
                }
            }

            float currentDisplacement = Math.Abs((float)Math.Sin(_currentPhase)) * _displacementMax * _currentDirection;
            Transform.LocalPosition = _origin + Vector3.UnitY * currentDisplacement;
        }
    }
}
