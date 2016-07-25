using System;
using Ge.Behaviors;
using Ge.Physics;
using ImGuiNET;
using Ge.Graphics;
using BEPUphysics;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Veldrid.Platform;

namespace Ge.Editor
{
    public class DebugPanel : Behavior
    {
        private Camera _camera;
        private PhysicsSystem _physics;
        private InputSystem _input;
        private GameObjectQuerySystem _goQuery;
        private GameObject _selectedObject;
        private bool _windowOpen = false;

        private Dictionary<Type, Action<Component>> _drawers;

        public DebugPanel(Camera camera)
        {
            _camera = camera;
            _drawers = new Dictionary<Type, Action<Component>>()
            {
                { typeof(Transform), DrawTransform },
                { typeof(Collider), DrawCollider },
                { typeof(MeshRenderer), DrawMeshRenderer },

                { typeof(Component), GenericDrawer },

            };
        }

        private void GenericDrawer(Component obj)
        {
            ImGui.Text(obj.GetType().Name);
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

        private static void DrawTransform(Component c)
        {
            Transform t = (Transform)c;
            Vector3 pos = t.LocalPosition;
            if (ImGui.DragVector3("Position", ref pos, -50f, 50f, 0.05f))
            {
                t.LocalPosition = pos;
            }
            Quaternion rotation = t.LocalRotation;

            float scale = t.LocalScale.X;
            if (ImGui.DragFloat("Scale", ref scale, .01f, 50f, 0.05f))
            {
                t.LocalScale = new Vector3(scale);
            }
        }

        protected override void Start(SystemRegistry registry)
        {
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
            _goQuery = registry.GetSystem<GameObjectQuerySystem>();
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetKeyDown(Key.Tilde))
            {
                _windowOpen = !_windowOpen;
            }

            if (_windowOpen)
            {
                if (_input.GetMouseButtonDown(MouseButton.Left) && !ImGui.IsMouseHoveringAnyWindow())
                {
                    var screenPos = _input.MousePosition;
                    var ray = _camera.GetRayFromScreenPoint(screenPos.X, screenPos.Y);

                    RayCastResult rcr;
                    if (_physics.Space.RayCast(ray, out rcr))
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
                    _selectedObject.Destroy();
                }

                Vector2 displaySize = ImGui.GetIO().DisplaySize / ImGui.GetIO().DisplayFramebufferScale;
                Vector2 size = new Vector2(
                    Math.Min(350, displaySize.X * 0.275f),
                    Math.Min(600, displaySize.Y * 0.75f));
                Vector2 pos = new Vector2(displaySize.X - size.X - 5, 5);

                ImGui.SetNextWindowSize(size, SetCondition.Always);
                ImGui.SetNextWindowPos(pos, SetCondition.Always);
                if (ImGui.BeginWindow("Viewer", WindowFlags.NoCollapse | WindowFlags.NoMove | WindowFlags.NoResize))
                {
                    if (_selectedObject != null)
                    {
                        DrawObject(_selectedObject);
                    }
                    else
                    {
                        DrawHierarchy();
                    }
                    ImGui.EndWindow();
                }
            }
        }

        private void DrawHierarchy()
        {
            foreach (var go in _goQuery.GetUnparentedGameObjects())
            {
                DrawNode(go.Transform);
            }
        }

        private void DrawNode(Transform t)
        {
            if (t.Children.Count > 0)
            {
                if (ImGui.TreeNode(t.GameObject.Name))
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
                }
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
            Action<Component> drawer = GetDrawer(type);
            if (ImGui.CollapsingHeader(type.Name, type.Name, false, true))
            {
                drawer.Invoke(component);

            }
        }

        private Action<Component> GetDrawer(Type type)
        {
            Action<Component> drawer;
            if (!_drawers.TryGetValue(type, out drawer))
            {
                foreach (var kvp in _drawers)
                {
                    if (kvp.Key.GetTypeInfo().IsAssignableFrom(type))
                    {
                        return kvp.Value;
                    }
                }

                throw new InvalidOperationException("No drawer found for " + type.Name);
            }
            else
            {
                return drawer;
            }
        }
    }
}
