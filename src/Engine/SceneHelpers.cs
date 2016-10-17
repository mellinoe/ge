using System;
using Engine.Assets;

namespace Engine
{
    public class SceneLoaderSystem : GameSystem
    {
        protected readonly GameObjectQuerySystem _goqs;

        private SceneAsset _loadedScene;

        public event Action BeforeSceneLoaded;
        public event Action AfterSceneLoaded;

        public SceneLoaderSystem(GameObjectQuerySystem goqs)
        {
            _goqs = goqs;
        }

        public void LoadScene(SceneAsset asset)
        {
            BeforeSceneLoaded?.Invoke();
            ClearCurrentSceneGameObjects();
            asset.GenerateGameObjects();
            _loadedScene = asset;
            AfterSceneLoaded?.Invoke();
        }

        protected virtual void ClearCurrentSceneGameObjects()
        {
            foreach (var go in _goqs.GetAllGameObjects())
            {
                go.Destroy();
            }
        }

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}
