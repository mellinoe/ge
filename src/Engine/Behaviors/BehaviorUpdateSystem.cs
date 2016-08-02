using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Engine.Behaviors
{
    public class BehaviorUpdateSystem : GameSystem
    {
        private readonly SystemRegistry _registry;

        private ImmutableList<IUpdateable> _behaviors = ImmutableList.Create<IUpdateable>();
        private List<Behavior> _newStarts = new List<Behavior>();

        public IEnumerable<IUpdateable> Updateables => _behaviors;

        public BehaviorUpdateSystem(SystemRegistry sr)
        {
            _registry = sr;
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            foreach (var b in _newStarts)
            {
                b.Start(_registry);
            }
            _newStarts.Clear();

            foreach (var behavior in _behaviors)
            {
                behavior.Update(deltaSeconds);
            }
        }

        public void Register(IUpdateable behavior)
        {
            _behaviors = _behaviors.Add(behavior);
            if (behavior is Behavior)
            _newStarts.Add((Behavior)behavior);
        }

        public void Remove(IUpdateable behavior)
        {
            _behaviors = _behaviors.Remove(behavior);
        }
    }
}
