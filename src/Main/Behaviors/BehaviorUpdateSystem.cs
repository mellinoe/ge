using System;
using System.Collections.Generic;

namespace Ge.Behaviors
{
    public class BehaviorUpdateSystem : GameSystem
    {
        private readonly List<IUpdateable> _behaviors = new List<IUpdateable>();

        public override void Update(float deltaSeconds)
        {
            foreach (var behavior in _behaviors)
            {
                behavior.Update(deltaSeconds);
            }
        }

        internal void Register(IUpdateable behavior)
        {
            _behaviors.Add(behavior);
        }

        internal void Remove(IUpdateable behavior)
        {
            _behaviors.Remove(behavior);
        }
    }
}
