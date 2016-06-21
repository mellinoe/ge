using System;
using Ge.Behaviors;
using Ge.Physics;
using ImGuiNET;
using Ge.Graphics;
using BEPUphysics;
using System.Numerics;
using System.Diagnostics;

namespace Ge.Editor
{
    public class DebugPanel : Behavior
    {
        private Camera _camera;
        private PhysicsSystem _physics;
        private InputSystem _input;
        private GameObject _selectedObject;
        private bool _windowOpen = true;

        public DebugPanel(Camera camera)
        {
            _camera = camera;
        }

        protected override void Start(SystemRegistry registry)
        {
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
        }

        public override void Update(float deltaSeconds)
        {
            if (_input.GetMouseButtonDown(OpenTK.Input.MouseButton.Left) && !ImGui.IsMouseHoveringAnyWindow())
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
                            if (_selectedObject == c.GameObject && _input.GetKey(OpenTK.Input.Key.ControlLeft))
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

            if (_selectedObject != null && _input.GetKeyDown(OpenTK.Input.Key.Delete))
            {
                _selectedObject.Destroy();
            }

            if (_windowOpen)
            {
                Vector2 displaySize = ImGui.GetIO().DisplaySize;
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
                    ImGui.EndWindow();
                }
            }
        }

        private void SelectObject(GameObject go)
        {
            ClearSelection();

            _selectedObject = go;
            _selectedObject.Destroyed += OnSelectedDestroyed;
            MeshRenderer mr = _selectedObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.Tint = new TintInfo(new Vector3(1.0f), 0.6f);
            }
        }

        private void ClearSelection()
        {
            if (_selectedObject != null)
            {
                _selectedObject.Destroyed -= OnSelectedDestroyed;
                MeshRenderer mr = _selectedObject.GetComponent<MeshRenderer>();
                if (mr != null)
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
                Vector3 pos = _selectedObject.Transform.LocalPosition;
                if (ImGui.DragVector3("Position", ref pos, -50f, 50f, 0.05f))
                {
                    _selectedObject.Transform.LocalPosition = pos;
                }
                float scale = _selectedObject.Transform.LocalScale.X;
                if (ImGui.DragFloat("Scale", ref scale, .01f, 50f, 0.05f))
                {
                    _selectedObject.Transform.LocalScale = new Vector3(scale);
                }
            }
        }
    }
}
