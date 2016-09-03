using System;
using Engine.Assets;

namespace Engine
{
    public class SceneLoaderSystem : GameSystem
    {
        protected readonly GameObjectQuerySystem _goqs;

        private SceneAsset _loadedScene;

        public SceneLoaderSystem(GameObjectQuerySystem goqs)
        {
            _goqs = goqs;
        }

        public void LoadScene(SceneAsset asset)
        {
            ClearCurrentSceneGameObjects();
            asset.GenerateGameObjects();
            _loadedScene = asset;
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
