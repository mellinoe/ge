using System;
using Engine.Behaviors;

namespace Engine.Editor
{
    public abstract class EditorBehavior : Component, IUpdateable
    {
        private EditorSystem _es;

        protected sealed override void Attached(SystemRegistry registry)
        {
            _es = registry.GetSystem<EditorSystem>();
            Start(registry);
        }

        protected sealed override void Removed(SystemRegistry registry)
        {
        }

        protected override void OnEnabled()
        {
            _es.RegisterBehavior(this);
        }

        protected override void OnDisabled()
        {
            _es.RemoveBehavior(this);
        }

        internal virtual void Start(SystemRegistry registry)
        {
        }

        public abstract void Update(float deltaSeconds);
    }
}
