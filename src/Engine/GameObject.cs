using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Engine
{
    public class GameObject
    {
        private readonly MultiValueDictionary<Type, Component> _components = new MultiValueDictionary<Type, Component>();
        private SystemRegistry _registry;
        private bool _enabled = true;

        public string Name { get; set; }

        public Transform Transform { get; }

        public bool Enabled
        {
            get { return _enabled; }
            set { if (value != _enabled) { SetEnabled(value); } }
        }

        internal static event Action<GameObject> GameObjectConstructed;
        internal static event Action<GameObject> GameObjectDestroyed;

        public event Action<GameObject> Destroyed;

        public GameObject() : this(Guid.NewGuid().ToString())
        { }

        public GameObject(string name)
        {
            Transform t = new Transform();
            AddComponent(t);
            Transform = t;
            Name = name;
            GameObjectConstructed?.Invoke(this);
        }

        public void AddComponent(Component component)
        {
            _components.Add(component.GetType(), component);
            component.AttachToGameObject(this, _registry);
        }

        public void AddComponent<T>(T component) where T : Component
        {
            _components.Add(typeof(T), component);
            component.AttachToGameObject(this, _registry);
        }

        public void RemoveAll<T>() where T : Component
        {
            var components = _components[typeof(T)];
            foreach (Component c in components)
            {
                c.Removed(_registry);
            }

            _components.Remove(typeof(T));
        }

        public void RemoveComponent<T>(T component) where T : Component
        {
            _components.Remove(typeof(T), component);
            component.Removed(_registry);
        }

        public void RemoveComponent(Component component)
        {
            _components.Remove(component.GetType(), component);
            component.Removed(_registry);
        }

        public T GetComponent<T>() where T : Component
        {
            IReadOnlyCollection<Component> components;
            if (!_components.TryGetValue(typeof(T), out components))
            {
                foreach (var kvp in _components)
                {
                    if (typeof(T).GetTypeInfo().IsAssignableFrom(kvp.Key))
                    {
                        if (kvp.Value.Any())
                        {
                            return (T)kvp.Value.First();
                        }
                    }
                }
            }
            else
            {
                return (T)components.First();
            }

            return null;
        }

        internal void SetRegistry(SystemRegistry systemRegistry)
        {
            _registry = systemRegistry;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            IReadOnlyCollection<Component> components;
            if (!_components.TryGetValue(typeof(T), out components))
            {
                foreach (var kvp in _components)
                {
                    if (typeof(T).GetTypeInfo().IsAssignableFrom(kvp.Key))
                    {
                        foreach (var comp in kvp.Value)
                        {
                            yield return (T)comp;
                        }
                    }
                }
            }
            else
            {
                foreach (var comp in components)
                {
                    yield return (T)comp;
                }
            }
        }

        public T GetComponentInParent<T>() where T : Component
        {
            T component;
            GameObject parent = this;
            while ((parent = parent.Transform.Parent?.GameObject) != null)
            {
                component = parent.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public void Destroy()
        {
            GameObjectDestroyed.Invoke(this);
        }

        internal void CommitDestroy()
        {
            foreach (var componentList in _components)
            {
                foreach (var component in componentList.Value)
                {
                    component.Removed(_registry);
                }
            }

            _components.Clear();

            Destroyed?.Invoke(this);
        }

        private void SetEnabled(bool state)
        {
            if (!state)
            {
                foreach (var component in GetComponents<Component>())
                {
                    component.Removed(_registry);
                }
            }
            else
            {
                foreach (var component in GetComponents<Component>())
                {
                    component.Attached(_registry);
                }
            }
            _enabled = state;
        }

        public override string ToString()
        {
            return $"{Name}, {_components.Values.Sum(irc => irc.Count)} components";
        }
    }
}