using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class GameObjectQuerySystem : GameSystem
    {
        private readonly IReadOnlyList<GameObject> _gameObjects;
        
        public GameObjectQuerySystem(IReadOnlyList<GameObject> gameObjectList)
        {
            _gameObjects = gameObjectList;
        }
        
        protected override void UpdateCore(float deltaSeconds)
        {
        }
        
        public GameObject FindByName(string name)
        {
            return _gameObjects.FirstOrDefault(go => go.Name == name);
        }
        
        public IEnumerable<GameObject> GetUnparentedGameObjects()
        {
            return _gameObjects.Where(go => go.Transform.Parent == null);
        }

        public IEnumerable<GameObject> GetAllGameObjects()
        {
            return _gameObjects;
        }

        public string GetCloneName(string name)
        {
            while (FindByName(name) != null)
            {
                name += " (Clone)";
            }

            return name;
        }
    }
}