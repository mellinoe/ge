using System;
using System.Collections.Generic;
using System.Linq;

namespace Ge
{
    public class GameObject
    {
        private readonly MultiValueDictionary<Type, object> _components = new MultiValueDictionary<Type, object>();

        public void AddComponent<T>(T component) where T : Component
        {
            _components.Add(typeof(T), component);
        }

        public T GetComponent<T>() where T : Component
        {
            IReadOnlyCollection<T> components;
            if (!_components.TryGetValue(typeof(T), out components))
            {
                return null;
            }

            return (T)components.First();
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            IEnumerable<T> components = Array.Empty<T>();
            _components.TryGetValue(typeof(T), out components);
            return components;
        }
    }
}