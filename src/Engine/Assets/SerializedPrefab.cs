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
            Dictionary<ulong, GameObject> prefabIDToGO = new Dictionary<ulong, GameObject>();

            foreach (SerializedGameObject sgo in GameObjects)
            {
                GameObject go = new GameObject(sgo.Name);
                go.Transform.LocalPosition = sgo.Transform.LocalPosition;
                go.Transform.LocalRotation = sgo.Transform.LocalRotation;
                go.Transform.LocalScale = sgo.Transform.LocalScale;

                foreach (var component in sgo.Components)
                {
                    go.AddComponent(component);
                }

                prefabIDToGO.Add(sgo.ID, go);
            }

            foreach (SerializedGameObject sgo in GameObjects)
            {
                GameObject go = prefabIDToGO[sgo.ID];
                ulong parentOriginalID = sgo.Transform.ParentID;
                if (parentOriginalID != 0)
                {
                    go.Transform.Parent = prefabIDToGO[parentOriginalID].Transform;
                }
            }

            return prefabIDToGO[GameObjects.First().ID];
        }
    }
}
