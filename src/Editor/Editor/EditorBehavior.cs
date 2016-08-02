using Engine.Behaviors;
using System;

namespace Engine.Editor
{
    public abstract class EditorBehavior : Component, IUpdateable
    {
        public override void Attached(SystemRegistry registry)
        {
            registry.GetSystem<EditorSystem>().RegisterBehavior(this);
            Start(registry);
        }

        public override void Removed(SystemRegistry registry)
        {
            registry.GetSystem<EditorSystem>().RemoveBehavior(this);
        }

        internal virtual void Start(SystemRegistry registry)
        {
        }

        public abstract void Update(float deltaSeconds);
    }
}
