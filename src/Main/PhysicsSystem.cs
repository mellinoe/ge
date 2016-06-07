using System;
using BEPUphysics;

namespace Ge
{
    public class PhysicsSystem : GameSystem
    {
        private readonly Space _space = new Space();

        public override void Update(float deltaSeconds)
        {
            _space.Update(deltaSeconds);
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            _space.Add(spaceObject);
        }
    }
}