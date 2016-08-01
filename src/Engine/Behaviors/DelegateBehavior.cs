using System;

namespace Engine.Behaviors
{
    public class DelegateBehavior : Behavior
    {
        private readonly Action<float> _updateAction;
        
        public DelegateBehavior(Action<float> updateAction)
        {
            _updateAction = updateAction;
        }
        
        public override void Update(float deltaSeconds)
        {
            _updateAction(deltaSeconds);
        }
    }
}