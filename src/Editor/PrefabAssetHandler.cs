using System;
using Engine.Assets;
using ImGuiNET;

namespace Engine.Editor
{
    public class PrefabAssetHandler : AssetMenuHandler<SerializedPrefab>
    {
        private readonly GameObjectQuerySystem _goqs;
        private readonly EditorSystem _es;

        public PrefabAssetHandler(GameObjectQuerySystem goqs, EditorSystem es)
        {
            _goqs = goqs;
            _es = es;
        }

        protected override void CoreDrawMenuItems(Func<SerializedPrefab> getAsset)
        {
            if (ImGui.MenuItem("Instantiate Prefab"))
            {
                GameObject go = getAsset().Instantiate(_goqs);
                _es.ClearSelection();
                _es.SelectObject(go);
            }
        }

        protected override void CoreHandleItemOpen(string path)
        {
            GenericAssetMenuHandler.GenericFileOpen(path);
        }
    }
}