namespace Engine.Behaviors
{
    public abstract class Behavior : Component, IUpdateable
    {
        private BehaviorUpdateSystem _bus;

        protected sealed override void Attached(SystemRegistry registry)
        {
            _bus = registry.GetSystem<BehaviorUpdateSystem>();
        }

        protected sealed override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            _bus.Register(this);
        }

        protected override void OnDisabled()
        {
            _bus.Remove(this);
        }

        public abstract void Update(float deltaSeconds);

        internal virtual void Start(SystemRegistry registry) { }
    }
}
