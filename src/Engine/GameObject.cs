using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Engine
{
    public class GameObject
    {
        private readonly MultiValueDictionary<Type, Component> _components = new MultiValueDictionary<Type, Component>();
        private SystemRegistry _registry;
        private bool _enabled = true;
        private bool _enabledInHierarchy = true;

        public string Name { get; set; }

        public Transform Transform { get; }

        public bool Enabled
        {
            get { return _enabled; }
            set { if (value != _enabled) { SetEnabled(value); } }
        }

        public bool EnabledInHierarchy => _enabledInHierarchy;

        internal static event Action<GameObject> InternalConstructed;
        internal static event Action<GameObject> InternalDestroyRequested;
        internal static event Action<GameObject> InternalDestroyCommitted;

        public event Action<GameObject> Destroyed;

        public GameObject() : this(Guid.NewGuid().ToString())
        { }

        public GameObject(string name)
        {
            Transform t = new Transform();
            t.ParentChanged += OnTransformParentChanged;
            AddComponent(t);
            Transform = t;
            Name = name;
            InternalConstructed?.Invoke(this);
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
                c.InternalRemoved(_registry);
            }

            _components.Remove(typeof(T));
        }

        public void RemoveComponent<T>(T component) where T : Component
        {
            component.InternalRemoved(_registry);
            _components.Remove(typeof(T), component);
        }

        public void RemoveComponent(Component component)
        {
            _components.Remove(component.GetType(), component);
            component.InternalRemoved(_registry);
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
                foreach (var kvp in _components.ToArray())
                {
                    if (typeof(T).GetTypeInfo().IsAssignableFrom(kvp.Key))
                    {
                        foreach (var comp in kvp.Value.ToArray())
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
            InternalDestroyRequested.Invoke(this);
        }

        internal void CommitDestroy()
        {
            foreach (var child in Transform.Children.ToArray())
            {
                child.GameObject.CommitDestroy();
            }

            foreach (var componentList in _components)
            {
                foreach (var component in componentList.Value)
                {
                    component.InternalRemoved(_registry);
                }
            }

            _components.Clear();

            Destroyed?.Invoke(this);
            InternalDestroyCommitted.Invoke(this);
        }

        private void SetEnabled(bool state)
        {
            _enabled = state;

            foreach (var child in Transform.Children)
            {
                child.GameObject.HierarchyEnabledStateChanged();
            }

            HierarchyEnabledStateChanged();
        }

        private void OnTransformParentChanged(Transform t, Transform oldParent, Transform newParent)
        {
            HierarchyEnabledStateChanged();
        }

        private void HierarchyEnabledStateChanged()
        {
            bool newState = _enabled && IsParentEnabled();
            if (_enabledInHierarchy != newState)
            {
                CoreHierarchyEnabledStateChanged(newState);
            }
        }

        private void CoreHierarchyEnabledStateChanged(bool newState)
        {
            Debug.Assert(newState != _enabledInHierarchy);
            _enabledInHierarchy = newState;
            foreach (var component in GetComponents<Component>())
            {
                component.HierarchyEnabledStateChanged();
            }
        }

        private bool IsParentEnabled()
        {
            return Transform.Parent == null || Transform.Parent.GameObject.Enabled;
        }

        public override string ToString()
        {
            return $"{Name}, {_components.Values.Sum(irc => irc.Count)} components";
        }
    }
}