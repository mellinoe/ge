using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine.Editor
{
    public abstract class AssetMenuHandler
    {
        public void DrawMenuItems(Func<object> getAsset)
        {
            CoreDrawMenuItems(getAsset);
        }

        public void HandleFileOpen(string path)
        {
            CoreHandleItemOpen(path);
        }

        public abstract Type TypeHandled { get; }

        protected abstract void CoreHandleItemOpen(string path);
        protected abstract void CoreDrawMenuItems(Func<object> getAsset);
    }

    public abstract class AssetMenuHandler<T> : AssetMenuHandler
    {
        private static readonly Type _typeHandled = typeof(T);
        public override Type TypeHandled => _typeHandled;
        protected override void CoreDrawMenuItems(Func<object> getAsset)
        {
            Func<T> func = () => (T)getAsset();
            CoreDrawMenuItems(func);
        }

        protected abstract void CoreDrawMenuItems(Func<T> getAsset);
    }

    public class ExplicitMenuHandler<T> : AssetMenuHandler<T>
    {
        private readonly Action _drawMenuItems;
        private readonly Action<string> _handleItemOpen;

        public ExplicitMenuHandler(Action drawMenuItems, Action<string> handleItemOpen)
        {
            _drawMenuItems = drawMenuItems;
            if (handleItemOpen == null)
            {
                handleItemOpen = GenericAssetMenuHandler.GenericFileOpen;
            }

            _handleItemOpen = handleItemOpen;
        }

        protected override void CoreDrawMenuItems(Func<T> getAsset)
        {
            _drawMenuItems();
        }

        protected override void CoreHandleItemOpen(string path)
        {
            _handleItemOpen(path);
        }
    }

    public class GenericAssetMenuHandler : AssetMenuHandler
    {
        public override Type TypeHandled => typeof(object);

        protected override void CoreDrawMenuItems(Func<object> getAsset)
        {
        }

        protected override void CoreHandleItemOpen(string path)
        {
            GenericFileOpen(path);
        }

        internal static void GenericFileOpen(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("cmd", $"/c {path}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", path);
            }
            else
            {
                throw new NotImplementedException("Cannot open items on this platform.");
            }
        }
    }
}
