using System;
using BEPUphysics;
using BEPUutilities.Threading;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Concurrent;

namespace Engine.Physics
{
    public class PhysicsSystem : GameSystem
    {
        private readonly ParallelLooper _looper;

        private BlockingCollection<ISpaceObject> _additions = new BlockingCollection<ISpaceObject>();
        private BlockingCollection<ISpaceObject> _removals = new BlockingCollection<ISpaceObject>();

        public Space Space { get; }

        public PhysicsSystem()
        {
            _looper = new ParallelLooper();
            for (int g = 0; g < Environment.ProcessorCount - 1; g++)
            {
                _looper.AddThread();
            }

            Space = new Space(_looper);
            Space.ForceUpdater.Gravity = new Vector3(0f, -9.81f, 0f);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            FlushAdditionsAndRemovals();
            Space.Update(deltaSeconds);
        }

        private void FlushAdditionsAndRemovals()
        {
            ISpaceObject addition;
            while (_additions.TryTake(out addition))
            {
                Space.Add(addition);
            }

            ISpaceObject removal;
            while (_removals.TryTake(out removal))
            {
                Space.Remove(removal);
            }
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            _additions.Add(spaceObject);
        }

        public void RemoveObject(ISpaceObject spaceObject)
        {
            _removals.Add(spaceObject);
        }
    }
}