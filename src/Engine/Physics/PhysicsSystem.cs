using System;
using BEPUphysics;
using BEPUutilities.Threading;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using BEPUphysics.CollisionRuleManagement;

namespace Engine.Physics
{
    public class PhysicsSystem : GameSystem
    {
        private const float MaxFrameUpdateTime = 30f / 60f;

        private static readonly Vector3 s_defaultGravity = new Vector3(0, -9.81f, 0);

        private PhysicsCollisionGroups _collisionGroups;
        private readonly ParallelLooper _looper;

        private BlockingCollection<ISpaceObject> _additions = new BlockingCollection<ISpaceObject>();
        private BlockingCollection<ISpaceObject> _removals = new BlockingCollection<ISpaceObject>();

        public Space Space { get; }
        public int LayerCount => _collisionGroups.GetLayerCount();
        public int GetLayerByName(string name) => _collisionGroups.GetLayerByName(name);

        public PhysicsSystem(PhysicsLayersDescription layers)
        {
            _collisionGroups = new PhysicsCollisionGroups(layers);
            CollisionRules.CollisionRuleCalculator = CustomCollisionRuleCalculator;
            _looper = new ParallelLooper();
            for (int g = 0; g < Environment.ProcessorCount - 1; g++)
            {
                _looper.AddThread();
            }

            Space = new Space(_looper);
            Space.ForceUpdater.Gravity = s_defaultGravity;
            Space.BufferedStates.Enabled = true;
            Space.BufferedStates.InterpolatedStates.Enabled = true;
        }

        public CollisionGroup GetCollisionGroup(int layer)
        {
            return _collisionGroups.GetCollisionGroup(layer);
        }

        public void SetPhysicsLayerRules(PhysicsLayersDescription layers)
        {
            _collisionGroups = new PhysicsCollisionGroups(layers);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            FlushAdditionsAndRemovals();

            deltaSeconds = Math.Min(deltaSeconds, MaxFrameUpdateTime);
            Space.Update(deltaSeconds);
        }

        public void AddObject(ISpaceObject spaceObject)
        {
            _additions.Add(spaceObject);
        }

        public void RemoveObject(ISpaceObject spaceObject)
        {
            _removals.Add(spaceObject);
        }

        protected override void OnNewSceneLoadedCore()
        {
            Space.ForceUpdater.Gravity = s_defaultGravity;
        }

        private CollisionRule CustomCollisionRuleCalculator(ICollisionRulesOwner aOwner, ICollisionRulesOwner bOwner)
        {
            CollisionRules a = aOwner.CollisionRules;
            CollisionRules b = bOwner.CollisionRules;
            CollisionRule groupRule = CollisionRules.GetGroupCollisionRuleDefault(a, b);
            if (groupRule == CollisionRule.NoBroadPhase)
            {
                return groupRule;
            }

            return CollisionRules.GetCollisionRuleDefault(aOwner, bOwner);
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
    }
}