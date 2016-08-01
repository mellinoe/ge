using System;
using Engine.Behaviors;
using Engine.Physics;
using ImGuiNET;
using Engine.Graphics;
using BEPUphysics;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Veldrid.Platform;
using System.Linq;
using Newtonsoft.Json;
using Veldrid.Graphics;
using Engine.Assets;
using System.IO;

namespace Engine.Editor
{
    public class DebugPanel : Behavior
    {
        private PhysicsSystem _physics;
        private InputSystem _input;
        private GameObjectQuerySystem _goQuery;
        private GameObject _selectedObject;
        private AssetSystem _as;
        private GraphicsSystem _gs;
        private bool _windowOpen = false;

        public DebugPanel()
        {
            DrawerCache.AddDrawer(new FuncDrawer<Transform>(DrawTransform));
            DrawerCache.AddDrawer(new FuncDrawer<Collider>(DrawCollider));
            DrawerCache.AddDrawer(new FuncDrawer<MeshRenderer>(DrawMeshRenderer));
            DrawerCache.AddDrawer(new FuncDrawer<Component>(GenericDrawer));
        }

        private static HashSet<string> s_ignoredGenericProps = new HashSet<string>()
        {
            "Transform",
            "GameObject"
        };

        private void GenericDrawer(Component obj)
        {
            TypeInfo t = obj.GetType().GetTypeInfo();
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(pi => !pi.IsDefined(typeof(JsonIgnoreAttribute)));
            foreach (var prop in props)
            {
                if (s_ignoredGenericProps.Contains(prop.Name))
                {
                    continue;
                }

                var drawer = DrawerCache.GetDrawer(prop.PropertyType);
                object value = prop.GetValue(obj);
                if (drawer.Draw(prop.Name, ref value, _gs.Context))
                {
                    if (prop.SetMethod != null)
                    {
                        prop.SetValue(obj, value);
                    }
                }
            }
        }

        private static void DrawMeshRenderer(Component c)
        {
            MeshRenderer mr = (MeshRenderer)c;
            bool wf = mr.Wireframe;
            if (ImGui.Checkbox("Wireframe", ref wf))
            {
                mr.Wireframe = wf;
            }

            bool dcbf = mr.DontCullBackFace;
            if (ImGui.Checkbox("Don't Cull Backface", ref dcbf))
            {
                mr.DontCullBackFace = dcbf;
            }
        }


        private static void DrawCollider(Component c)
        {
            Collider collider = (Collider)c;
            float mass = collider.Entity.Mass;
            if (ImGui.DragFloat("Mass", ref mass, 0f, 1000f, .1f))
            {
                collider.Entity.Mass = mass;
            }

            bool trigger = collider.IsTrigger;
            if (ImGui.Checkbox("Is Trigger", ref trigger))
            {
                collider.IsTrigger = trigger;
            }
        }

        private static void DrawTransform(Transform t)
        {
            Vector3 pos = t.LocalPosition;
            if (ImGui.DragVector3("Position", ref pos, -50f, 50f, 0.05f))
            {
                t.LocalPosition = pos;
            }
            object rotation = t.LocalRotation;
            var drawer = DrawerCache.GetDrawer(typeof(Quaternion));
            if (drawer.Draw("Rotation", ref rotation, null))
            {
                t.Rotation = (Quaternion)rotation;
            }

            float scale = t.LocalScale.X;
            if (ImGui.DragFloat("Scale", ref scale, .01f, 50f, 0.05f))
            {
                t.LocalScale = new Vector3(scale);
            }
        }

        internal override void Start(SystemRegistry registry)
        {
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
            _goQuery = registry.GetSystem<GameObjectQuerySystem>();
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = registry.GetSystem<AssetSystem>();
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetKeyDown(Key.F1))
            {
                _windowOpen = !_windowOpen;
            }

            if (_windowOpen)
            {
                if (_input.GetMouseButtonDown(MouseButton.Left) && !ImGui.IsMouseHoveringAnyWindow())
                {
                    var screenPos = _input.MousePosition;
                    var ray = _gs.MainCamera.GetRayFromScreenPoint(screenPos.X, screenPos.Y);

                    RayCastResult rcr;
                    if (_physics.Space.RayCast(ray, (bpe) => bpe.Tag != null && bpe.Tag is Collider, out rcr))
                    {
                        if (rcr.HitObject.Tag != null)
                        {
                            Collider c = rcr.HitObject.Tag as Collider;
                            if (c != null)
                            {
                                if (_selectedObject == c.GameObject && _input.GetKey(Key.ControlLeft))
                                {
                                    ClearSelection();
                                }
                                else
                                {
                                    SelectObject(c.GameObject);
                                }
                            }
                        }
                    }
                    else
                    {
                        ClearSelection();
                    }
                }

                if (_selectedObject != null && _input.GetKeyDown(Key.Delete))
                {
                    DeleteGameObject(_selectedObject);
                }

                // Hierarchy Editor
                {
                    Vector2 displaySize = ImGui.GetIO().DisplaySize / ImGui.GetIO().DisplayFramebufferScale;
                    Vector2 size = new Vector2(
                        Math.Min(350, displaySize.X * 0.275f),
                        Math.Min(600, displaySize.Y * 0.75f));
                    Vector2 pos = new Vector2(5, 5);
                    ImGui.SetNextWindowSize(size, SetCondition.Always);
                    ImGui.SetNextWindowPos(pos, SetCondition.Always);

                    if (ImGui.BeginWindow("Scene Hierarchy", WindowFlags.NoCollapse | WindowFlags.NoMove | WindowFlags.NoResize))
                    {
                        DrawHierarchy();
                    }
                    ImGui.EndWindow();
                }

                // Component Editor
                {
                    Vector2 displaySize = ImGui.GetIO().DisplaySize / ImGui.GetIO().DisplayFramebufferScale;
                    Vector2 size = new Vector2(
                        Math.Min(350, displaySize.X * 0.275f),
                        Math.Min(600, displaySize.Y * 0.75f));
                    Vector2 pos = new Vector2(displaySize.X - size.X - 5, 5);
                    ImGui.SetNextWindowSize(size, SetCondition.Always);
                    ImGui.SetNextWindowPos(pos, SetCondition.Always);

                    if (ImGui.BeginWindow("Component Viewer", WindowFlags.NoCollapse | WindowFlags.NoMove | WindowFlags.NoResize))
                    {
                        if (_selectedObject != null)
                        {
                            DrawObject(_selectedObject);
                        }
                    }
                    ImGui.EndWindow();
                }
            }
        }

        private void DrawHierarchy()
        {
            IEnumerable<GameObject> rootObjects = _goQuery.GetUnparentedGameObjects().ToArray();
            foreach (var go in rootObjects)
            {
                DrawNode(go.Transform);
            }
        }

        private void DrawNode(Transform t)
        {
            bool isSelected = t.GameObject == _selectedObject;
            if (isSelected)
            {
                ImGui.PushStyleColor(ColorTarget.Text, RgbaFloat.Cyan.ToVector4());
            }
            if (t.Children.Count > 0)
            {
                bool opened = ImGui.TreeNode($"##{t.GameObject.Name}");
                ImGui.SameLine();
                if (ImGui.Selectable(t.GameObject.Name))
                {
                    SelectObject(t.GameObject);
                }
                if (isSelected)
                {
                    ImGui.PopStyleColor();
                }

                if (opened)
                {
                    foreach (var child in t.Children)
                    {
                        DrawNode(child);
                    }

                    ImGui.TreePop();
                }
            }
            else
            {
                if (ImGui.Selectable(t.GameObject.Name))
                {
                    SelectObject(t.GameObject);
                }
                if (isSelected)
                {
                    ImGui.PopStyleColor();
                }
                if (ImGui.BeginPopupContextItem($"{t.GameObject.Name}_Context"))
                {
                    if (ImGui.MenuItem("Clone", string.Empty))
                    {
                        CloneGameObject(t.GameObject);
                    }
                    if (ImGui.MenuItem("Delete", string.Empty))
                    {
                        DeleteGameObject(t.GameObject);
                    }
                    ImGui.EndPopup();
                }
            }
        }

        private void DeleteGameObject(GameObject go)
        {
            go.Destroy();
        }

        private void CloneGameObject(GameObject go)
        {
            SerializedGameObject sgo = new SerializedGameObject(go);
            using (var fs = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096))
            {
                var textWriter = new JsonTextWriter(new StreamWriter(fs));
                _as.Database.DefaultSerializer.Serialize(textWriter, sgo);
                textWriter.Flush();
                fs.Seek(0, SeekOrigin.Begin);
                var reader = new JsonTextReader(new StreamReader(fs));
                sgo = _as.Database.DefaultSerializer.Deserialize<SerializedGameObject>(reader);
            }

            GameObject newGo = new GameObject($"{go.Name} (Clone)");
            newGo.Transform.LocalPosition = sgo.Transform.LocalPosition;
            newGo.Transform.LocalRotation = sgo.Transform.LocalRotation;
            newGo.Transform.LocalScale = sgo.Transform.LocalScale;

            foreach (var comp in sgo.Components)
            {
                newGo.AddComponent(comp);
            }

            if (go.Transform.Parent != null)
            {
                newGo.Transform.Parent = go.Transform.Parent;
            }
        }

        private void SelectObject(GameObject go)
        {
            ClearSelection();

            _selectedObject = go;
            _selectedObject.Destroyed += OnSelectedDestroyed;
            var mrs = _selectedObject.GetComponents<MeshRenderer>();
            foreach (var mr in mrs)
            {
                mr.Tint = new TintInfo(new Vector3(1.0f), 0.6f);
            }
        }

        private void ClearSelection()
        {
            if (_selectedObject != null)
            {
                _selectedObject.Destroyed -= OnSelectedDestroyed;
                var mrs = _selectedObject.GetComponents<MeshRenderer>();
                foreach (var mr in mrs)
                {
                    mr.Tint = new TintInfo();
                }

                _selectedObject = null;
            }
        }

        void OnSelectedDestroyed(GameObject go)
        {
            Debug.Assert(go == _selectedObject);
            _selectedObject = null;
        }

        private void DrawObject(GameObject _selectedObject)
        {
            if (ImGui.CollapsingHeader(_selectedObject.Name, _selectedObject.Name, true, true))
            {
                foreach (var component in _selectedObject.GetComponents<Component>())
                {
                    DrawComponent(component);
                }
            }
        }

        private void DrawComponent(Component component)
        {
            var type = component.GetType();
            var drawer = DrawerCache.GetDrawer(type);
            object componentAsObject = component;
            if (ImGui.CollapsingHeader(type.Name, type.Name, false, true))
            {
                drawer.Draw(type.Name, ref componentAsObject, _gs.Context);
            }
        }
    }
}
