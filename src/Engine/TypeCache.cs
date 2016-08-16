using System;
using System.Collections.Generic;
using System.Reflection;

namespace Engine
{
    public class TypeCache<T>
    {
        private readonly Dictionary<Type, T> _items = new Dictionary<Type, T>();
        private readonly Dictionary<Type, T> _fallbackItems = new Dictionary<Type, T>();

        public void AddItem(Type t, T item)
        {
            _items.Add(t, item);
            _fallbackItems.Clear();
        }

        public T GetItem(Type type)
        {
            T d;
            if (!_items.TryGetValue(type, out d))
            {
                d = GetFallbackItem(type);
                if (d == null)
                {
                    throw new InvalidOperationException($"No T compatible with {type.Name}");
                }
            }

            return d;
        }

        private T GetFallbackItem(Type type)
        {
            T item = default(T);
            if (_fallbackItems.TryGetValue(type, out item))
            {
                return item;
            }
            else
            {
                int hierarchyDistance = int.MaxValue;
                foreach (var kvp in _items)
                {
                    if (kvp.Key.IsAssignableFrom(type))
                    {
                        int newHD = GetHierarchyDistance(kvp.Key.GetTypeInfo(), type.GetTypeInfo());
                        if (newHD < hierarchyDistance)
                        {
                            hierarchyDistance = newHD;
                            item = kvp.Value;
                        }
                    }
                }

                _fallbackItems.Add(type, item);
                return item;
            }
        }

        private int GetHierarchyDistance(TypeInfo baseType, TypeInfo derived)
        {
            int distance = 0;
            while ((derived = derived.BaseType?.GetTypeInfo()) != null)
            {
                distance++;
                if (derived == baseType)
                {
                    return distance;
                }
            }

            throw new InvalidOperationException($"{baseType.Name} is not a superclass of {derived.Name}");
        }
    }
}
