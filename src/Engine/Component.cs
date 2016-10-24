using System;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Engine
{
    public abstract class Component
    {
        [JsonIgnore]
        private GameObject _attachedGO;
        [JsonIgnore]
        private Transform _transform;

        [JsonIgnore]
        public GameObject GameObject => _attachedGO;

        [JsonIgnore]
        public Transform Transform => _transform;

        internal void AttachToGameObject(GameObject go, SystemRegistry registry)
        {
            _attachedGO = go;
            _transform = _attachedGO.Transform;
            InternalAttached(registry);
        }

        private bool _enabled = true;
        private bool _enabledInHierarchy = false;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    HierarchyEnabledStateChanged();
                }
            }
        }

        public bool EnabledInHierarchy => _enabledInHierarchy;

        internal void HierarchyEnabledStateChanged()
        {
            bool newState = _enabled && GameObject.Enabled;
            if (newState != _enabledInHierarchy)
            {
                CoreHierarchyEnabledStateChanged(newState);
            }
        }

        private void CoreHierarchyEnabledStateChanged(bool newState)
        {
            Debug.Assert(newState != _enabledInHierarchy);
            _enabledInHierarchy = newState;
            if (newState)
            {
                OnEnabled();
            }
            else
            {
                OnDisabled();
            }
        }

        internal void InternalAttached(SystemRegistry registry)
        {
            Attached(registry);
            HierarchyEnabledStateChanged();
        }

        internal void InternalRemoved(SystemRegistry registry)
        {
            if (_enabledInHierarchy)
            {
                OnDisabled();
            }

            Removed(registry);
        }

        protected abstract void OnEnabled();
        protected abstract void OnDisabled();

        protected abstract void Attached(SystemRegistry registry);
        protected abstract void Removed(SystemRegistry registry);

        public override string ToString()
        {
            return $"{GetType().Name}, {GameObject.Name}";
        }
    }
}