using System;
using BEPUphysics;
using BEPUutilities.Threading;
using System.Numerics;

namespace Ge.Physics
{
    public class PhysicsSystem : GameSystem
    {
        private readonly Space _space;
        private readonly ParallelLooper _looper;

        public PhysicsSystem()
        {
            _looper = new ParallelLooper();
            for (int g = 0; g < Environment.ProcessorCount - 1; g++)
            {
                _looper.AddThread();
            }

            _space = new Space(_looper);
            _space.ForceUpdater.Gravity = new Vector3(0f, -9.81f, 0f);
        }

        public override void Update(float deltaSeconds)
        {
            _space.Update(deltaSeconds);
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            _space.Add(spaceObject);
        }

        internal void RemoveObject(ISpaceObject spaceObect)
        {
            _space.Remove(spaceObect);
        }
    }
}