using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Engine.Assets
{
    public class SceneAsset
    {
        public SerializedGameObject[] GameObjects { get; set; }

        public void GenerateGameObjects()
        {
            Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();
            Dictionary<string, SerializedGameObject> sGameObjects = new Dictionary<string, SerializedGameObject>();

            foreach (var sgo in GameObjects)
            {
                GameObject go = new GameObject(sgo.Name);
                go.Transform.LocalPosition = sgo.Transform.LocalPosition;
                go.Transform.LocalRotation = sgo.Transform.LocalRotation;
                go.Transform.LocalScale = sgo.Transform.LocalScale;

                foreach (var component in sgo.Components)
                {
                    go.AddComponent(component);
                }

                gameObjects.Add(go.Name, go);
                sGameObjects.Add(go.Name, sgo);
            }

            foreach (var kvp in sGameObjects)
            {
                string parentName = kvp.Value.Transform.ParentName;
                if (!string.IsNullOrEmpty(parentName))
                {
                    var parent = gameObjects[parentName];
                    gameObjects[kvp.Key].Transform.Parent = parent.Transform;
                }
            }
        }
    }

    public class SerializedGameObject
    {
        private static HashSet<Type> s_excludedComponents = new HashSet<Type>()
        {
            typeof(Transform)
        };

        public string Name { get; set; }
        public SerializedTransform Transform { get; set; }
        public Component[] Components { get; set; }

        public SerializedGameObject()
        {
        }

        public SerializedGameObject(GameObject go)
        {
            Name = go.Name;
            Transform = new SerializedTransform(go.Transform);
            Components = go.GetComponents<Component>().Where(c => !s_excludedComponents.Contains(c.GetType())).ToArray();
        }
    }

    public class SerializedTransform
    {
        public Vector3 LocalPosition { get; set; }
        public Quaternion LocalRotation { get; set; }
        public Vector3 LocalScale { get; set; }
        public string ParentName { get; set; }

        public SerializedTransform()
        {
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public SerializedTransform(Transform transform)
        {
            LocalPosition = transform.LocalPosition;
            LocalRotation = transform.LocalRotation;
            LocalScale = transform.LocalScale;

            if (transform.Parent != null)
            {
                ParentName = transform.Parent.GameObject.Name;
            }
            else
            {
                ParentName = string.Empty;
            }
        }
    }
}
