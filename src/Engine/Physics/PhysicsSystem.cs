using System;
using BEPUphysics;
using BEPUutilities.Threading;
using System.Numerics;

namespace Ge.Physics
{
    public class PhysicsSystem : GameSystem
    {
        private readonly ParallelLooper _looper;

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

        public override void Update(float deltaSeconds)
        {
            Space.Update(deltaSeconds);
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            Space.Add(spaceObject);
        }

        internal void RemoveObject(ISpaceObject spaceObect)
        {
            Space.Remove(spaceObect);
        }
    }
}