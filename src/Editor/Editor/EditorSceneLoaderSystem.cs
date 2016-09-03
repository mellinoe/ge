using System.Linq;

namespace Engine.Editor
{
    public class EditorSceneLoaderSystem : SceneLoaderSystem
    {
        public EditorSceneLoaderSystem(GameObjectQuerySystem goqs) : base(goqs)
        {
        }

        protected override void ClearCurrentSceneGameObjects()
        {
            foreach (var go in _goqs.GetAllGameObjects().Where(go => !EditorUtility.IsEditorObject(go)))
            {
                go.Destroy();
            }
        }
    }
}
