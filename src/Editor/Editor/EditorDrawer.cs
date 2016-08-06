using Engine.Editor.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Veldrid.Graphics;

namespace Engine.Editor
{
    public abstract class EditorDrawer
    {
        public Type TypeDrawn { get; }
        public abstract Command Draw(string label, object target, RenderContext rc);

        public EditorDrawer(Type typeDrawn)
        {
            TypeDrawn = typeDrawn;
        }
    }

    public abstract class EditorDrawer<T> : EditorDrawer
    {
        public EditorDrawer() : base(typeof(T)) { }

        public override Command Draw(string label, object target, RenderContext rc)
        {
            return Draw(label, (T)target, rc);
        }

        protected abstract Command Draw(string label, T target, RenderContext rc);
    }

    public delegate Command EditorDrawFunc<T>(string label, T obj, RenderContext rc) where T : class;
    public class FuncEditorDrawer<T> : EditorDrawer<T> where T : class
    {
        private readonly EditorDrawFunc<T> _drawFunc;

        public FuncEditorDrawer(Func<T, Command> drawFunc)
        {
            _drawFunc = (label, obj, rc) =>
            {
                return drawFunc(obj);
            };
        }

        public FuncEditorDrawer(EditorDrawFunc<T> drawFunc)
        {
            _drawFunc = drawFunc;
        }

        protected override Command Draw(string label, T target, RenderContext rc)
        {
            return _drawFunc(label, target, rc);
        }
    }

    public static class EditorDrawerCache
    {
        private static Dictionary<Type, EditorDrawer> s_drawers = new Dictionary<Type, EditorDrawer>()
        {

        };

        // Drawers which should be used when type doesn't identically match queried type.
        private static Dictionary<Type, EditorDrawer> s_fallbackDrawers = new Dictionary<Type, EditorDrawer>();

        public static void AddDrawer(EditorDrawer drawer)
        {
            s_drawers.Add(drawer.TypeDrawn, drawer);
            s_fallbackDrawers.Clear();
        }

        public static EditorDrawer GetDrawer(Type type)
        {
            EditorDrawer d;
            if (!s_drawers.TryGetValue(type, out d))
            {
                d = GetFallbackDrawer(type);
                if (d == null)
                {
                    throw new InvalidOperationException($"No EditorDrawer compatible with {type.Name}");
                }
            }

            return d;
        }

        private static EditorDrawer GetFallbackDrawer(Type type)
        {
            EditorDrawer d = null;
            if (s_fallbackDrawers.TryGetValue(type, out d))
            {
                return d;
            }
            else
            {
                int hierarchyDistance = int.MaxValue;
                foreach (var kvp in s_drawers)
                {
                    if (kvp.Key.IsAssignableFrom(type))
                    {
                        int newHD = GetHierarchyDistance(kvp.Key.GetTypeInfo(), type.GetTypeInfo());
                        if (newHD < hierarchyDistance)
                        {
                            hierarchyDistance = newHD;
                            d = kvp.Value;
                        }
                    }
                }

                s_fallbackDrawers.Add(type, d);
                return d;
            }
        }

        private static int GetHierarchyDistance(TypeInfo baseType, TypeInfo derived)
        {
            int distance = 0;
            while ((derived = derived.BaseType?.GetTypeInfo()) != null)
            {
                distance++;
                if (derived == baseType)
                {
                    return distance;
                }
            }

            throw new InvalidOperationException($"{baseType.Name} is not a superclass of {derived.Name}");
        }
    }
}
