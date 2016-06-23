using System;
using System.Collections.Generic;
using System.Linq;

namespace Ge
{
    public class GameObjectQuerySystem : GameSystem
    {
        private readonly IReadOnlyList<GameObject> _gameObjects;
        
        public GameObjectQuerySystem(IReadOnlyList<GameObject> gameObjectList)
        {
            _gameObjects = gameObjectList;
        }
        
        public override void Update(float deltaSeconds)
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
    }
}