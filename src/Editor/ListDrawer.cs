using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Veldrid.Graphics;

namespace Engine.Editor
{
    public class ListDrawer<T> : Drawer<List<T>>
    {
        public override bool Draw(string label, ref List<T> obj, RenderContext rc)
        {
            var arrayDrawer = DrawerCache.GetDrawer(typeof(T[]));
            object arrayAsObj = obj.ToArray();
            if (arrayDrawer.Draw(label, ref arrayAsObj, rc))
            {
                T[] array = (T[])arrayAsObj;
                obj = new List<T>(array);
                return true;
            }

            return false;
        }
    }
}