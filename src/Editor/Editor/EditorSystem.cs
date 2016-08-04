using BEPUphysics;
using Engine.Assets;
using Engine.Behaviors;
using Engine.Graphics;
using Engine.Physics;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Veldrid.Graphics;
using Veldrid.Platform;
using Veldrid.Assets;

namespace Engine.Editor
{
    public class EditorSystem : GameSystem
    {
        private static HashSet<string> s_ignoredGenericProps = new HashSet<string>()
        {
            "Transform",
            "GameObject"
        };
        private static readonly char[] s_pathTrimChar = new char[]
        {
            '\"',
            '\''
        };

        private readonly SystemRegistry _registry;
        private PhysicsSystem _physics;
        private InputSystem _input;
        private GameObjectQuerySystem _goQuery;
        private GameObject _selectedObject;
        private AssetSystem _as;
        private GraphicsSystem _gs;
        private bool _windowOpen = false;
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<EditorBehavior> _newStarts = new List<EditorBehavior>();
        private readonly TextInputBuffer _filenameInputBuffer = new TextInputBuffer(256);
        private BehaviorUpdateSystem _bus;
        private Camera _sceneCam;
        private readonly GameObject _editorCameraGO;
        private readonly Camera _editorCamera;

        public EditorSystem(SystemRegistry registry)
        {
            _registry = registry;
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
            _goQuery = registry.GetSystem<GameObjectQuerySystem>();
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = registry.GetSystem<AssetSystem>();
            _bus = registry.GetSystem<BehaviorUpdateSystem>();

            DrawerCache.AddDrawer(new FuncDrawer<Transform>(DrawTransform));
            DrawerCache.AddDrawer(new FuncDrawer<Collider>(DrawCollider));
            DrawerCache.AddDrawer(new FuncDrawer<MeshRenderer>(DrawMeshRenderer));
            DrawerCache.AddDrawer(new FuncDrawer<Component>(GenericDrawer));

            _registry.Register(this);

            _editorCameraGO = new GameObject("__EditorCamera");

            _sceneCam = _gs.MainCamera;
            _editorCamera = new Camera()
            {
                FarPlaneDistance = 200
            };
            _editorCameraGO.AddComponent(_editorCamera);
            _editorCameraGO.AddComponent(new EditorCameraMovement());

            _physics.Update(1f / 60f);
            _physics.Enabled = false;
            _bus.Enabled = false;

            var imGuiRenderer = _bus.Updateables.Single(u => u is ImGuiRenderer);
            _bus.Remove(imGuiRenderer);
            RegisterBehavior(imGuiRenderer);
        }

        public void StartSimulation()
        {
            _bus.Enabled = true;
            _physics.Enabled = true;
            if (_sceneCam != null)
            {
                _editorCameraGO.Enabled = false;
                _sceneCam.GameObject.Enabled = true;
                _gs.SetMainCamera(_sceneCam);
            }
        }

        public void PauseSimulation()
        {
            _bus.Enabled = false;
            _physics.Enabled = false;
            if (_sceneCam != null)
            {
                _sceneCam.GameObject.Enabled = false;
                _editorCameraGO.Enabled = true;
                _gs.SetMainCamera(_editorCamera);
            }
        }

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

                Drawer drawer;
                object value = prop.GetValue(obj);
                if (value == null)
                {
                    drawer = DrawerCache.GetDrawer(prop.PropertyType);
                }
                else
                {
                    drawer = DrawerCache.GetDrawer(value.GetType());
                }
                if (drawer.Draw(prop.Name, ref value, _gs.Context))
                {
                    if (prop.SetMethod != null)
                    {
                        prop.SetValue(obj, value);
                    }
                }
            }
        }

        private void DrawMeshRenderer(Component c)
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

            if (!mr.Texture.HasValue)
            {
                var result = DrawTextureRef(mr.Texture.GetRef(), _as.Database);
                if (result != null)
                {
                    mr.Texture = result;
                }
            }
        }

        private AssetRef<TextureData> DrawTextureRef(AssetRef<TextureData> tex, LooseFileDatabase database)
        {
            AssetID result = default(AssetID);
            var assets = database.GetAssetsOfType(typeof(TextureData));
            foreach (var id in assets)
            {
                if (ImGui.Button(id.Value))
                {
                    result = id;
                }
            }

            if (result != default(AssetID))
            {
                return new AssetRef<TextureData>(result);
            }
            else
            {
                return null;
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

        protected override void UpdateCore(float deltaSeconds)
        {
            UpdateUpdateables(deltaSeconds);

            DrawMainMenu();

            if (_input.GetKeyDown(Key.F1))
            {
                _windowOpen = !_windowOpen;
            }
            if (_input.GetKeyDown(Key.F11))
            {
                ToggleWindowFullscreenState();
            }
            if (_input.GetKeyDown(Key.F12))
            {
                _gs.ToggleOctreeVisualizer();
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
                    Vector2 pos = new Vector2(0, 20);
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
                    Vector2 pos = new Vector2(displaySize.X - size.X, 20);
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

        private void DrawMainMenu()
        {
            bool openPopup = false;

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open Scene"))
                    {
                        openPopup = true;
                    }
                    if (!string.IsNullOrEmpty(EditorPreferences.Instance.LastOpenedScene))
                    {
                        if (ImGui.MenuItem($"Last Opened: {EditorPreferences.Instance.LastOpenedScene}"))
                        {
                            LoadScene(EditorPreferences.Instance.LastOpenedScene);
                        }
                    }
                    if (ImGui.MenuItem("Exit"))
                    {
                        ExitEditor();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("View"))
                {
                    if (ImGui.MenuItem("Full Screen", "F11"))
                    {
                        ToggleWindowFullscreenState();
                    }
                    if (ImGui.MenuItem("Scene Octree Visualizer", "F12"))
                    {
                        _gs.ToggleOctreeVisualizer();
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Game"))
                {
                    if (!_bus.Enabled)
                    {
                        if (ImGui.MenuItem("Play", "Ctrl-P") || (ImGui.GetIO().KeysDown[(int)Key.P] && ImGui.GetIO().CtrlPressed))
                        {
                            StartSimulation();
                        }
                    }
                    else
                    {
                        if (ImGui.MenuItem("Pause", "Ctrl-P"))
                        {
                            PauseSimulation();
                        }
                    }

                    ImGui.EndMenu();
                }


                ImGui.EndMainMenuBar();
            }

            if (_input.GetKeyDown(Key.P) && (_input.GetKey(Key.ControlLeft) || _input.GetKey(Key.ControlRight)))
            {
                if (!_bus.Enabled)
                {
                    StartSimulation();
                }
                else
                {
                    PauseSimulation();
                }
            }


            if (openPopup)
            {
                ImGui.OpenPopup("###OpenScenePopup");
            }

            if (ImGui.BeginPopup("###OpenScenePopup"))
            {
                ImGui.Text("Path to scene file:");
                if (openPopup)
                {
                    ImGuiNative.igSetKeyboardFocusHere(0);
                }
                if (ImGui.InputText(string.Empty, _filenameInputBuffer.Buffer, _filenameInputBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    LoadScene(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    LoadScene(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void ToggleWindowFullscreenState()
        {
            Window window = _gs.Context.Window;
            WindowState state = window.WindowState;
            window.WindowState = state != WindowState.FullScreen ? WindowState.FullScreen : WindowState.Normal;
        }

        private void ExitEditor()
        {
            _gs.Context.Window.Close();
        }

        private void LoadScene(string path)
        {
            path = path.Trim(s_pathTrimChar);
            Console.WriteLine("Open scene: " + path);
            SceneAsset sa = null;
            using (var fs = File.OpenRead(path))
            {
                var jtr = new JsonTextReader(new StreamReader(fs));
                sa = _as.Database.DefaultSerializer.Deserialize<SceneAsset>(jtr);
            }

            string projectRoot = Path.GetDirectoryName(path);
            _gs.Context.ResourceFactory.ShaderAssetRootPath = projectRoot;
            _as.Database.RootPath = Path.Combine(projectRoot, "Assets");

            sa.GenerateGameObjects();
            DoPhysicsTick();

            _sceneCam = _gs.MainCamera;

            _gs.SetMainCamera(_editorCamera);

            EditorPreferences.Instance.LastOpenedScene = path;
        }

        private void DoPhysicsTick()
        {
            _physics.Space.Update();
        }

        private void UpdateUpdateables(float deltaSeconds)
        {
            foreach (var b in _newStarts)
            {
                b.Start(_registry);
            }
            _newStarts.Clear();

            foreach (var updateable in _updateables)
            {
                updateable.Update(deltaSeconds);
            }
        }

        private void DrawHierarchy()
        {
            IEnumerable<GameObject> rootObjects = _goQuery.GetUnparentedGameObjects().OrderBy(go => go.Name).ToArray();
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

        public void RegisterBehavior(IUpdateable behavior)
        {
            _updateables.Add(behavior);
            if (behavior is EditorBehavior)
            {
                _newStarts.Add((EditorBehavior)behavior);
            }
        }

        public void RemoveBehavior(IUpdateable behavior)
        {
            _updateables.Remove(behavior);
        }
    }
}
