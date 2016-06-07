namespace Ge.Behaviors
{
    public abstract class Behavior : Component, IUpdateable
    {
        public sealed override void Attached(SystemRegistry registry)
        {
            registry.GetSystem<BehaviorUpdateSystem>().Register(this);
        }

        public sealed override void Removed(SystemRegistry registry)
        {
            registry.GetSystem<BehaviorUpdateSystem>().Remove(this);
        }

        public abstract void Update(float deltaSeconds);
    }
}
