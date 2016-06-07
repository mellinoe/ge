namespace Ge
{
    public abstract class Component
    {
        private GameObject _attachedGO;

        internal void AttachToGameObject(GameObject go)
        {
            _attachedGO = go;
        }

        protected abstract void Initialize(SystemRegistry registry);
    }
}