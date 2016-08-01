using System;

namespace Engine.Behaviors
{
    public class TimedDeath : Behavior
    {
        private float _remaining;

        public TimedDeath(float lifespanInSeconds)
        {
            _remaining = lifespanInSeconds;
        }

        public override void Update(float deltaSeconds)
        {
            _remaining -= deltaSeconds;
            if (_remaining <= 0)
            {
                GameObject.Destroy();
            }
        }
    }
}
