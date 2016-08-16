using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine.Editor
{
    public abstract class AssetMenuHandler
    {
        public void DrawMenuItems()
        {
            CoreDrawMenuItems();
        }

        public void HandleFileOpen(string path)
        {
            CoreHandleItemOpen(path);
        }

        public abstract Type TypeHandled { get; }

        protected abstract void CoreHandleItemOpen(string path);
        protected abstract void CoreDrawMenuItems();
    }

    public abstract class AssetMenuHandler<T> : AssetMenuHandler
    {
        private static readonly Type _typeHandled = typeof(T);
        public override Type TypeHandled => _typeHandled;
    }

    public class ExplicitMenuHandler<T> : AssetMenuHandler<T>
    {
        private readonly Action _drawMenuItems;
        private readonly Action<string> _handleItemOpen;

        public ExplicitMenuHandler(Action drawMenuItems, Action<string> handleItemOpen)
        {
            _drawMenuItems = drawMenuItems;
            _handleItemOpen = handleItemOpen;
        }

        protected override void CoreDrawMenuItems()
        {
            _drawMenuItems();
        }

        protected override void CoreHandleItemOpen(string path)
        {
            _handleItemOpen(path);
        }
    }

    public class GenericAssetMenuHandler : AssetMenuHandler<object>
    {
        protected override void CoreDrawMenuItems()
        {
        }

        protected override void CoreHandleItemOpen(string path)
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
