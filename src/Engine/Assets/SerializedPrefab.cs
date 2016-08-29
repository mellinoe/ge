using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Assets
{
    public class SerializedPrefab
    {
        public SerializedGameObject[] GameObjects { get; set; }

        public SerializedPrefab(IEnumerable<GameObject> gameObjects)
        {
            GameObjects = gameObjects.Select(go => new SerializedGameObject(go)).ToArray();
        }

        [JsonConstructor]
        internal SerializedPrefab(SerializedGameObject[] gameObjects)
        {
            GameObjects = gameObjects;
        }

        public GameObject Instantiate(GameObjectQuerySystem goqs)
        {
            Dictionary<string, GameObject> prefabNameToGO = new Dictionary<string, GameObject>();

            foreach (var sgo in GameObjects)
            {
                string clonedName = goqs.GetCloneName(sgo.Name);
                GameObject go = new GameObject(clonedName);
                go.Transform.LocalPosition = sgo.Transform.LocalPosition;
                go.Transform.LocalRotation = sgo.Transform.LocalRotation;
                go.Transform.LocalScale = sgo.Transform.LocalScale;

                foreach (var component in sgo.Components)
                {
                    go.AddComponent(component);
                }

                prefabNameToGO.Add(sgo.Name, go);
            }

            foreach (var kvp in GameObjects)
            {
                GameObject go = prefabNameToGO[kvp.Name];
                string parentOriginalName = kvp.Transform.ParentName;
                if (!string.IsNullOrEmpty(parentOriginalName))
                {
                    go.Transform.Parent = prefabNameToGO[parentOriginalName].Transform;
                }
            }

            return prefabNameToGO[GameObjects.First().Name];
        }
    }
}
