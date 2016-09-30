namespace Engine.Behaviors
{
    public abstract class Behavior : Component, IUpdateable
    {
        private BehaviorUpdateSystem _bus;

        protected sealed override void Attached(SystemRegistry registry)
        {
            _bus = registry.GetSystem<BehaviorUpdateSystem>();
            PostAttached(registry);
        }

        protected sealed override void Removed(SystemRegistry registry)
        {
            PostRemoved(registry);
        }

        protected sealed override void OnEnabled()
        {
            _bus.Register(this);
            PostEnabled();
        }

        protected sealed override void OnDisabled()
        {
            _bus.Remove(this);
            PostDisabled();
        }

        public abstract void Update(float deltaSeconds);

        internal void StartInternal(SystemRegistry registry) => Start(registry);
        protected virtual void Start(SystemRegistry registry) { }
        protected virtual void PostEnabled() { }
        protected virtual void PostDisabled() { }
        protected virtual void PostAttached(SystemRegistry registry) { }
        protected virtual void PostRemoved(SystemRegistry registry) { }
    }
}
