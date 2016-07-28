using System;
using System.Collections.Generic;

namespace Ge
{
    public class SystemRegistry
    {
        private Dictionary<Type, GameSystem> _systems = new Dictionary<Type, GameSystem>();

        public T GetSystem<T>() where T : GameSystem
        {
            GameSystem gs;
            if (!_systems.TryGetValue(typeof(T), out gs))
            {
                throw new InvalidOperationException($"No system of type {typeof(T).Name} was found.");
            }
            
            return (T)gs;
        }
        
        public void Register<T>(T system) where T : GameSystem
        {
            _systems.Add(typeof(T), system);
        }

        public IReadOnlyCollection<KeyValuePair<Type, GameSystem>> GetSystems()
        {
            return _systems;
        }
    }
}