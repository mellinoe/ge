using Engine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Engine.Assets
{
    public class SceneAsset
    {
        public string Name { get; set; }

        public SerializedGameObject[] GameObjects { get; set; }

        public void GenerateGameObjects(bool parallel = false)
        {
            ConcurrentDictionary<ulong, GameObject> idToGO = new ConcurrentDictionary<ulong, GameObject>();

            if (parallel)
            {
                Task.WaitAll(GameObjects.Select((sgo) => Task.Run(() =>
                {
                    GameObject go = new GameObject(sgo.Name);
                    go.Transform.LocalPosition = sgo.Transform.LocalPosition;
                    go.Transform.LocalRotation = sgo.Transform.LocalRotation;
                    go.Transform.LocalScale = sgo.Transform.LocalScale;

                    foreach (var component in sgo.Components)
                    {
                        go.AddComponent(component);
                    }

                    if (!idToGO.TryAdd(sgo.ID, go))
                    {
                        throw new InvalidOperationException("Multiple objects with the same ID were detected. ID = " + sgo.ID);
                    }
                })).ToArray());
            }
            else
            {
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

                    if (!idToGO.TryAdd(sgo.ID, go))
                    {
                        throw new InvalidOperationException("Multiple objects with the same ID were detected. ID = " + sgo.ID);
                    }
                }
            }
            foreach (var sgo in GameObjects)
            {
                ulong parentID = sgo.Transform.ParentID;
                if (parentID != 0)
                {
                    var parent = idToGO[parentID];
                    idToGO[sgo.ID].Transform.Parent = parent.Transform;
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
        public ulong ID { get; set; }

        public SerializedGameObject()
        {
        }

        public SerializedGameObject(GameObject go)
        {
            Name = go.Name;
            Transform = new SerializedTransform(go.Transform);
            Components = go.GetComponents<Component>().Where(c => !s_excludedComponents.Contains(c.GetType())).ToArray();
            ID = go.ID;
        }
    }

    public class SerializedTransform
    {
        public Vector3 LocalPosition { get; set; }
        public Quaternion LocalRotation { get; set; }
        public Vector3 LocalScale { get; set; }
        public ulong ParentID { get; set; }

        public SerializedTransform()
        {
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public SerializedTransform(Transform transform)
        {
            LocalPosition = transform.GetLocalOrPhysicsEntityPosition();
            LocalRotation = transform.GetLocalOrPhysicsEntityRotation();
            LocalScale = transform.LocalScale;

            if (transform.Parent != null)
            {
                ParentID = transform.Parent.GameObject.ID;
            }
        }
    }
}
