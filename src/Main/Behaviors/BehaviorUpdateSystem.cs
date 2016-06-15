using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Ge.Behaviors
{
    public class BehaviorUpdateSystem : GameSystem
    {
        private ImmutableList<IUpdateable> _behaviors = ImmutableList.Create<IUpdateable>();

        public override void Update(float deltaSeconds)
        {
            foreach (var behavior in _behaviors)
            {
                behavior.Update(deltaSeconds);
            }
        }

        internal void Register(IUpdateable behavior)
        {
            _behaviors = _behaviors.Add(behavior);
        }

        internal void Remove(IUpdateable behavior)
        {
            _behaviors = _behaviors.Remove(behavior);
        }
    }
}
