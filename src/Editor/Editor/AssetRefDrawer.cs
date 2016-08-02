using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Editor
{
    public class AssetRefDrawer<T> : Drawer<AssetRef<T>>
    {
        public override bool Draw(string label, ref AssetRef<T> obj, RenderContext rc)
        {
            object assetName = obj.ID.Value;
            if (DrawerCache.GetDrawer(typeof(string)).Draw(label, ref assetName, rc))
            {
                obj = new AssetRef<T>((string)assetName);
                return true;
            }

            return false;
        }
    }
}
