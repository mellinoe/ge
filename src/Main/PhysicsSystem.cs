using System;
using BEPUphysics;

namespace Ge
{
    public class PhysicsSystem : GameSystem
    {
        private readonly Space _space;

        public override void Update()
        {
            _space.Update();
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            _space.Add(spaceObject);
        }
    }
}