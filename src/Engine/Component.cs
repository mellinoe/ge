using Newtonsoft.Json;

namespace Ge
{
    public abstract class Component
    {
        [JsonIgnore]
        private GameObject _attachedGO;

        [JsonIgnore]
        public GameObject GameObject => _attachedGO;

        [JsonIgnore]
        public Transform Transform => _attachedGO.Transform;

        internal void AttachToGameObject(GameObject go, SystemRegistry registry)
        {
            _attachedGO = go;
            Attached(registry);
        }

        public abstract void Attached(SystemRegistry registry);
        public abstract void Removed(SystemRegistry registry);
    }
}