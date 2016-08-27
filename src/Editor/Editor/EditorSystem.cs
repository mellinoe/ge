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
using Engine.Editor.Commands;
using Engine.GUI;
using Engine.Editor.Graphics;
using Engine.ProjectSystem;

namespace Engine.Editor
{
    public class EditorSystem : GameSystem
    {
        private const string NewProjectManifestName = "NewProject.manifest";

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
        private static readonly HashSet<Type> s_newComponentExclusions = new HashSet<Type>()
        {
            typeof(Transform)
        };
        private readonly List<Type> _newComponentOptions = new List<Type>();
        private readonly HashSet<Type> _projectComponentsDiscovered = new HashSet<Type>();

        private readonly FrameTimeAverager _fta = new FrameTimeAverager(666);
        private bool _windowOpen = true;
        private PlayState _playState = PlayState.Stopped;

        private readonly SystemRegistry _registry;
        private readonly PhysicsSystem _physics;
        private readonly InputSystem _input;
        private readonly GameObjectQuerySystem _goQuery;
        private readonly EditorAssetSystem _as;
        private readonly GraphicsSystem _gs;
        private readonly BehaviorUpdateSystem _bus;
        private readonly AssemblyLoadSystem _als;

        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<EditorBehavior> _newStarts = new List<EditorBehavior>();

        private readonly TextInputBuffer _filenameInputBuffer = new TextInputBuffer(256);
        private readonly TextInputBuffer _filenameBuffer = new TextInputBuffer(100);
        private readonly TextInputBuffer _goNameBuffer = new TextInputBuffer(100);

        private Camera _sceneCam;
        private readonly GameObject _editorCameraGO;
        private readonly Camera _editorCamera;

        private HashSet<GameObject> _selectedObjects = new HashSet<GameObject>();
        private readonly UndoRedoStack _undoRedo = new UndoRedoStack();
        private Transform _multiTransformDummy = new Transform();
        private AxesRenderer _axesRenderer;

        private ProjectContext _projectContext;
        private ProjectPublisher _projectPublisher = new ProjectPublisher();

        private InMemoryAsset<SceneAsset> _currentScene;
        private string _currentScenePath;
        private readonly Vector4 _disabledGrey = new Vector4(0.65f, 0.65f, 0.65f, 0.35f);

        // Asset editor stuff
        private string _loadedAssetPath;
        private object _selectedAsset;
        private TypeCache<AssetMenuHandler> _assetMenuHandlers = new TypeCache<AssetMenuHandler>();

        // Status Bar
        private int _statusBarHeight = 20;
        private string _statusBarText = string.Empty;
        private Vector4 _statusBarTextColor;
        private GameObject _parentingTarget;
        private GameObject _newSelectedObject;

        public EditorSystem(SystemRegistry registry)
        {
            _registry = registry;
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
            _goQuery = registry.GetSystem<GameObjectQuerySystem>();
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = (EditorAssetSystem)registry.GetSystem<AssetSystem>();
            _bus = registry.GetSystem<BehaviorUpdateSystem>();
            _als = registry.GetSystem<AssemblyLoadSystem>();

            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<Transform>(DrawTransform));
            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<MeshRenderer>(DrawMeshRenderer));
            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<Component>(GenericDrawer));

            DrawerCache.AddDrawer(new FuncDrawer<RefOrImmediate<ImageProcessorTexture>>(DrawTextureRef));

            var genericHandler = new GenericAssetMenuHandler(); _assetMenuHandlers.AddItem(genericHandler.TypeHandled, genericHandler);
            var sceneHandler = new ExplicitMenuHandler<SceneAsset>(() => { }, (path) => LoadScene(path));
            _assetMenuHandlers.AddItem(sceneHandler.TypeHandled, sceneHandler);

            _registry.Register(this);

            _editorCameraGO = new GameObject("__EditorCamera");

            _sceneCam = _gs.MainCamera;
            _editorCamera = new Camera()
            {
                FarPlaneDistance = 200
            };
            _editorCameraGO.AddComponent(_editorCamera);
            _editorCameraGO.AddComponent(new EditorCameraMovement());

            DoFakePhysicsUpdate();
            _physics.Enabled = false;
            _bus.Enabled = false;

            var imGuiRenderer = _bus.Updateables.Single(u => u is ImGuiRenderer);
            _bus.Remove(imGuiRenderer);
            RegisterBehavior(imGuiRenderer);

            _axesRenderer = new AxesRenderer(_gs.Context, _gs);
            _gs.AddFreeRenderItem(_axesRenderer);

            DiscoverComponentsFromAssembly(typeof(Game).GetTypeInfo().Assembly);

            if (!string.IsNullOrEmpty(EditorPreferences.Instance.LastOpenedProjectRoot))
            {
                if (LoadProject(EditorPreferences.Instance.LastOpenedProjectRoot))
                {
                    var latestScene = EditorPreferences.Instance.GetLastOpenedScene(_projectContext.ProjectRootPath);
                    if (!string.IsNullOrEmpty(latestScene))
                    {
                        LoadScene(latestScene);
                    }
                }
            }
        }

        public IEnumerable<Type> DiscoverComponentsFromAssembly(Assembly assembly)
        {
            IEnumerable<Type> discovered = assembly.GetTypes()
                .Where(t => typeof(Component).IsAssignableFrom(t) && HasParameterlessConstructor(t) && !s_newComponentExclusions.Contains(t));
            _newComponentOptions.AddRange(discovered);
            return discovered;
        }

        private bool HasParameterlessConstructor(Type t)
        {
            return t.GetConstructor(Array.Empty<Type>()) != null;
        }

        public void StartSimulation()
        {
            if (_playState != PlayState.Playing)
            {
                if (_sceneCam != null)
                {
                    _editorCameraGO.Enabled = false;
                    _sceneCam.Enabled = true;
                }
                else
                {
                    Console.WriteLine("No camera in the current scene.");
                }

                if (_playState == PlayState.Stopped)
                {
                    SerializeGameObjectsToScene();
                }

                _playState = PlayState.Playing;

                _bus.Enabled = true;
                _physics.Enabled = true;
            }
        }

        public void PauseSimulation()
        {
            if (_playState != PlayState.Paused)
            {
                _playState = PlayState.Paused;

                _bus.Enabled = false;
                _physics.Enabled = false;
                if (_sceneCam != null)
                {
                    _sceneCam.Enabled = false;
                    _editorCameraGO.Enabled = true;
                }
            }
        }

        public void StopSimulation()
        {
            if (_playState != PlayState.Stopped)
            {
                _playState = PlayState.Stopped;

                _bus.Enabled = false;
                _physics.Enabled = false;
                if (_currentScene != null)
                {
                    DestroyNonEditorGameObjects();
                    ActivateCurrentScene();
                }
            }
        }

        private Command GenericDrawer(string label, Component obj, RenderContext rc)
        {
            Command c = null;

            TypeInfo t = obj.GetType().GetTypeInfo();
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(pi => !pi.IsDefined(typeof(JsonIgnoreAttribute)) && pi.SetMethod != null);
            foreach (var prop in props)
            {
                if (s_ignoredGenericProps.Contains(prop.Name))
                {
                    continue;
                }

                Drawer drawer;
                object currentValue = prop.GetValue(obj);
                object value = currentValue;
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
                        c = new ReflectionSetCommand(new PropertySettable(prop), obj, currentValue, value);
                    }
                }
            }

            return c;
        }

        private Command DrawMeshRenderer(string label, Component component, RenderContext rc)
        {
            Command c = null;

            MeshRenderer mr = (MeshRenderer)component;
            bool wf = mr.Wireframe;
            if (ImGui.Checkbox("Wireframe", ref wf))
            {
                c = SetValueActionCommand.New<bool>(val => mr.Wireframe = val, mr.Wireframe, wf);
            }

            bool dcbf = mr.DontCullBackFace;
            if (ImGui.Checkbox("Don't Cull Backface", ref dcbf))
            {
                c = SetValueActionCommand.New<bool>(val => mr.DontCullBackFace = val, mr.DontCullBackFace, dcbf);
            }

            if (!mr.Mesh.HasValue)
            {
                AssetRef<MeshData> result = DrawAssetRef("Model", mr.Mesh.GetRef(), _as.Database);
                if (result != null)
                {
                    c = SetValueActionCommand.New<AssetRef<MeshData>>(val => mr.Mesh = val, mr.Mesh.GetRef(), result);
                    mr.Mesh = result;
                }
            }

            if (!mr.Texture.HasValue)
            {
                AssetRef<TextureData> result = DrawAssetRef("Surface Texture", mr.Texture.GetRef(), _as.Database);
                if (result != null)
                {
                    c = SetValueActionCommand.New<AssetRef<TextureData>>(val => mr.Texture = val, mr.Texture.GetRef(), result);
                    mr.Texture = result;
                }
            }

            if (ImGui.Button("Toggle Bounds Renderer"))
            {
                mr.ToggleBoundsRenderer();
            }

            return c;
        }

        private bool DrawTextureRef(string label, ref RefOrImmediate<ImageProcessorTexture> obj, RenderContext rc)
        {
            AssetRef<ImageProcessorTexture> oldRef = obj.GetRef() ?? new AssetRef<ImageProcessorTexture>();
            AssetRef<ImageProcessorTexture> newRef = DrawAssetRef(label, oldRef, _as.Database);
            if (newRef != null)
            {
                obj = new RefOrImmediate<ImageProcessorTexture>(new AssetRef<ImageProcessorTexture>(newRef.ID), null);
                return true;
            }

            return false;
        }

        private static AssetRef<T> DrawAssetRef<T>(string label, AssetRef<T> existingRef, AssetDatabase database)
        {
            AssetID result = default(AssetID);
            AssetID[] assets = database.GetAssetsOfType(typeof(T));

            string[] items = assets.Select(id =>
            {
                return id.Value.Replace("Internal:", string.Empty);
            }).ToArray();
            int selected = 0;
            for (int i = 1; i < items.Length; i++)
            {
                if (existingRef.ID == assets[i]) { selected = i; break; }
            }
            if (ImGui.Combo(label, ref selected, items))
            {
                result = assets[selected];
            }

            if (result != default(AssetID) && result != existingRef.ID)
            {
                return new AssetRef<T>(result);
            }
            else
            {
                return null;
            }
        }

        private static Command DrawTransform(string label, Transform t, RenderContext rc)
        {
            Command c = null;

            Vector3 pos = t.LocalPosition;
            if (ImGui.DragVector3("Position", ref pos, -50f, 50f, 0.05f))
            {
                c = SetValueActionCommand.New<Vector3>((val) => t.LocalPosition = val, t.LocalPosition, pos);
            }

            object rotation = t.LocalRotation;
            var drawer = DrawerCache.GetDrawer(typeof(Quaternion));
            if (drawer.Draw("Rotation", ref rotation, null))
            {
                c = SetValueActionCommand.New<Quaternion>((val) => t.LocalRotation = val, t.LocalRotation, rotation);
            }

            float scale = t.LocalScale.X;
            if (ImGui.DragFloat("Scale", ref scale, .01f, 50f, 0.05f))
            {
                c = SetValueActionCommand.New<Vector3>((val) => t.LocalScale = val, t.LocalScale, new Vector3(scale));
            }

            return c;
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds * 1000.0);
            _gs.Context.Window.Title = $"ge.Editor " + _fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTime.ToString("#00.00 ms");
            UpdateUpdateables(deltaSeconds);
            DoFakePhysicsUpdate();

            if (_selectedObjects.Any())
            {
                _axesRenderer.Scale = Vector3.One * 2;
                _axesRenderer.Position = _selectedObjects.First().Transform.Position;
                _axesRenderer.Rotation = _selectedObjects.First().Transform.Rotation;
            }
            else
            {
                _axesRenderer.Scale = Vector3.Zero;
            }

            DrawMainMenu();
            DrawStatusBar();

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
            if (_input.GetKeyDown(Key.F3))
            {
                DoFakePhysicsUpdate();
            }

            // Undo-Redo
            if (_input.GetKeyDown(Key.Z) && _input.GetKey(Key.ControlLeft))
            {
                _undoRedo.UndoLatest();
            }
            if (_input.GetKeyDown(Key.Y) && _input.GetKey(Key.ControlLeft))
            {
                _undoRedo.RedoLatest();
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
                                GameObject go = c.GameObject;
                                GameObjectClicked(go);
                            }
                        }
                    }
                    else
                    {
                        ClearSelection();
                    }
                }

                if (_selectedObjects.Any() && _input.GetKeyDown(Key.Delete))
                {
                    DeleteGameObjects(_selectedObjects);
                }

                // Project Asset Editor
                {
                    Vector2 displaySize = ImGui.GetIO().DisplaySize;
                    Vector2 size = new Vector2(
                        Math.Min(350, displaySize.X * 0.275f),
                        Math.Min(600, displaySize.Y * 0.6f));
                    Vector2 pos = new Vector2(0, 20);
                    ImGui.SetNextWindowSize(size, SetCondition.Always);
                    ImGui.SetNextWindowPos(pos, SetCondition.Always);

                    if (ImGui.BeginWindow("Project Assets", WindowFlags.NoCollapse | WindowFlags.NoMove | WindowFlags.NoResize))
                    {
                        DrawProjectAssets();
                    }
                    ImGui.EndWindow();
                }

                // Hierarchy Editor
                {
                    Vector2 displaySize = ImGui.GetIO().DisplaySize;
                    Vector2 size = new Vector2(
                        Math.Min(350, displaySize.X * 0.275f),
                        Math.Min(600, (displaySize.Y * 0.35f) - (_statusBarHeight + 20)));
                    Vector2 pos = new Vector2(0, displaySize.Y * 0.6f + 20);
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
                    Vector2 displaySize = ImGui.GetIO().DisplaySize;
                    Vector2 size = new Vector2(
                        Math.Min(350, displaySize.X * 0.275f),
                        Math.Min(600, displaySize.Y * 0.75f));
                    Vector2 pos = new Vector2(displaySize.X - size.X, 20);
                    ImGui.SetNextWindowSize(size, SetCondition.Always);
                    ImGui.SetNextWindowPos(pos, SetCondition.Always);

                    if (ImGui.BeginWindow("Viewer", WindowFlags.NoCollapse | WindowFlags.NoMove | WindowFlags.NoResize))
                    {
                        DrawComponentViewer();
                    }
                    ImGui.EndWindow();
                }
            }
        }

        private void DrawStatusBar()
        {
            IO io = ImGui.GetIO();
            Vector2 pos = new Vector2(0, io.DisplaySize.Y - _statusBarHeight);
            ImGui.SetNextWindowPos(pos, SetCondition.Always);
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, _statusBarHeight), SetCondition.Always);
            ImGui.PushStyleVar(StyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(StyleVar.WindowPadding, new Vector2());
            ImGui.PushStyleVar(StyleVar.WindowMinSize, new Vector2());
            Vector4 statusBarColor = _playState == PlayState.Playing ? RgbaFloat.Orange.ToVector4()
                : _playState == PlayState.Paused ? RgbaFloat.Cyan.ToVector4() : RgbaFloat.Black.ToVector4();
            ImGui.PushStyleColor(ColorTarget.WindowBg, statusBarColor);
            if (ImGui.BeginWindow(
                string.Empty,
                WindowFlags.NoTitleBar | WindowFlags.NoResize | WindowFlags.NoScrollbar | WindowFlags.NoCollapse))
            {
                ImGui.PushStyleColor(ColorTarget.Text, _statusBarTextColor);
                ImGui.Text(_statusBarText);
                ImGui.PopStyleColor();
                ImGui.SameLine();
                var available = ImGui.GetContentRegionAvailableWidth();
                string stateText = $"State: {_playState.ToString()}";
                float start = available - ImGui.GetTextSize(stateText).X - 10;
                ImGui.SameLine(0, start);
                ImGui.Text(stateText);
            }
            ImGui.EndWindow();

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }

        private void DrawComponentViewer()
        {
            if (_selectedAsset != null)
            {
                Drawer d = DrawerCache.GetDrawer(_selectedAsset.GetType());
                d.Draw(_selectedAsset.GetType().Name, ref _selectedAsset, _gs.Context);

                ImGui.Text("Asset Name:");
                ImGui.PushItemWidth(220);
                ImGui.SameLine();
                if (ImGui.InputText("###AssetNameInput", _filenameBuffer.Buffer, _filenameBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    _loadedAssetPath = _filenameBuffer.StringValue;
                }
                ImGui.PopItemWidth();
                ImGui.SameLine();
                if (ImGui.Button("Save"))
                {
                    string path = _as.ProjectDatabase.GetAssetPath(_loadedAssetPath);
                    using (var fs = File.CreateText(path))
                    {
                        var serializer = JsonSerializer.CreateDefault();
                        serializer.TypeNameHandling = TypeNameHandling.All;
                        serializer.Serialize(fs, _selectedAsset);
                    }
                }
            }
            else if (_selectedObjects.Count > 1)
            {
                MultiDrawObjects(_selectedObjects);
            }
            else if (_selectedObjects.Count == 1)
            {
                DrawSingleObject(_selectedObjects.Single());
            }
        }

        private void DrawProjectAssets()
        {
            if (!string.IsNullOrEmpty(_projectContext?.ProjectRootPath))
            {
                DrawRecursiveNode(_as.ProjectDatabase.GetRootDirectoryGraph(), false);
            }
        }

        private void DrawRecursiveNode(DirectoryNode node, bool pushTreeNode)
        {
            if (!pushTreeNode || ImGui.TreeNode(node.FolderName))
            {
                foreach (DirectoryNode child in node.Children)
                {
                    DrawRecursiveNode(child, pushTreeNode: true);
                }

                foreach (AssetInfo asset in node.AssetInfos)
                {
                    if (ImGui.Selectable(asset.Name, _loadedAssetPath == asset.Path) && _loadedAssetPath != asset.Path)
                    {
                        ClearSelection();
                        _selectedAsset = _as.ProjectDatabase.LoadAsset(asset.Path);
                        _loadedAssetPath = asset.Path;
                        _filenameBuffer.StringValue = asset.Name;
                    }
                    if (_loadedAssetPath == asset.Path)
                    {
                        if (ImGui.GetIO().KeysDown[(int)Key.Enter])
                        {
                            Type assetType = _as.ProjectDatabase.GetAssetType(asset.Path);
                            AssetMenuHandler handler = _assetMenuHandlers.GetItem(assetType);
                            handler.HandleFileOpen(asset.Path);
                        }
                    }
                    if (ImGui.IsLastItemHovered())
                    {
                        ImGui.SetTooltip(asset.Path);
                    }
                    if (ImGui.BeginPopupContextItem(asset.Name + "_context"))
                    {
                        Type assetType = _as.ProjectDatabase.GetAssetType(asset.Path);
                        AssetMenuHandler handler = _assetMenuHandlers.GetItem(assetType);

                        if (ImGui.MenuItem("Open"))
                        {
                            handler.HandleFileOpen(asset.Path);
                        }
                        if (ImGui.MenuItem("Clone"))
                        {
                            _as.ProjectDatabase.CloneAsset(asset.Path);
                        }
                        if (ImGui.MenuItem("Delete"))
                        {
                            _as.ProjectDatabase.DeleteAsset(asset.Path);
                        }

                        handler.DrawMenuItems();

                        ImGui.EndPopup();
                    }
                }

                if (pushTreeNode)
                {
                    ImGui.TreePop();
                }
            }
        }

        private void GameObjectClicked(GameObject go)
        {
            if (_input.GetKey(Key.ControlLeft))
            {
                if (_selectedObjects.Contains(go))
                {
                    Deselect(go);
                }
                else
                {
                    SelectObject(go);
                }
            }
            else
            {
                ClearSelection();
                SelectObject(go);
            }
        }

        private void DoFakePhysicsUpdate()
        {
            //Vector3 gravity = _physics.Space.ForceUpdater.Gravity;
            //_physics.Space.ForceUpdater.Gravity = Vector3.Zero;
            //_physics.Space.Update();
            //_physics.Space.ForceUpdater.Gravity = gravity;

            var space = _physics.Space;
            space.BroadPhase.Update();
            space.NarrowPhase.Update();
            space.BoundingBoxUpdater.Update();
        }

        private void DrawMainMenu()
        {
            string openPopup = null;

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open Project"))
                    {
                        openPopup = "OpenProjectPopup";
                    }
                    {
                        string[] history =
                            _projectContext != null
                                ? EditorPreferences.Instance.GetProjectSceneHistory(_projectContext.ProjectRootPath).ToArray()
                                : Array.Empty<string>();
                        if (ImGui.BeginMenu($"Recently Opened", history.Any()))
                        {
                            foreach (string path in history.Reverse())
                            {
                                if (ImGui.MenuItem(path))
                                {
                                    if (!LoadScene(path))
                                    {
                                        StatusBarText("[!] Couldn't load scene: " + path, RgbaFloat.Red);
                                        EditorPreferences.Instance.OpenedSceneHistory.Remove(path);
                                    }
                                    else
                                    {
                                        StatusBarText("Successfully loaded scene: " + path, RgbaFloat.Green);
                                    }
                                }
                            }

                            ImGui.EndMenu();
                        }
                    }

                    ImGui.Separator();
                    if (ImGui.MenuItem("Save Scene", "Ctrl+S", false, _currentScene != null && _playState == PlayState.Stopped))
                    {
                        SaveCurrentScene(_currentScenePath);
                    }
                    if (ImGui.MenuItem("Save Scene As", "Ctrl+S", false, _currentScene != null && _playState == PlayState.Stopped))
                    {
                        openPopup = "SaveSceneAsPopup";
                    }
                    if (ImGui.MenuItem("Close Scene", string.Empty, false, _currentScene != null))
                    {
                        StopSimulation();
                        DestroyNonEditorGameObjects();
                        _currentScene = null;
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
                if (ImGui.BeginMenu("GameObject"))
                {
                    if (ImGui.MenuItem("Create Empty"))
                    {
                        var newGo = CreateEmptyGameObject();
                        ClearSelection();
                        SelectObject(newGo);
                    }
                    if (ImGui.MenuItem("Create Empty Child", _selectedObjects.Any()))
                    {
                        var newChild = CreateEmptyGameObject(_selectedObjects.First().Transform);
                        ClearSelection();
                        SelectObject(newChild);
                    }
                    if (ImGui.MenuItem("Create Empty Parent", _selectedObjects.Any()))
                    {
                        Vector3 position = MathUtil.SumAll(_selectedObjects.Select(go => go.Transform.Position)) / _selectedObjects.Count;
                        var newParent = CreateEmptyGameObject();
                        newParent.Transform.Position = position;
                        Command c = new RawCommand(() =>
                        {
                            foreach (var selected in _selectedObjects)
                            {
                                selected.Transform.Parent = newParent.Transform;
                            }
                        }, () =>
                        {
                            foreach (var selected in _selectedObjects)
                            {
                                selected.Transform.Parent = null;
                            }
                        });
                        _undoRedo.CommitCommand(c);
                        ClearSelection();
                        SelectObject(newParent);
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Unparent Selected", _selectedObjects.Any(go => go.Transform.Parent != null)))
                    {
                        foreach (var selected in _selectedObjects.Where(go => go.Transform.Parent != null))
                        {
                            selected.Transform.Parent = null;
                        }
                    }
                    if (ImGui.MenuItem("Select Parenting Target", _selectedObjects.Count == 1))
                    {
                        _parentingTarget = _selectedObjects.First();
                    }
                    if (ImGui.IsLastItemHovered())
                    {
                        ImGui.SetTooltip("Selects a GameObject to be used when parenting items with the next menu option.");
                    }
                    if (_parentingTarget != null)
                    {
                        if (ImGui.MenuItem($"Parent Selected to {_parentingTarget.Name}", _selectedObjects.Any(go => go != _parentingTarget)))
                        {
                            foreach (var selected in _selectedObjects.Where(go => go != _parentingTarget))
                            {
                                selected.Transform.Parent = _parentingTarget.Transform;
                            }
                        }
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Project"))
                {
                    if (ImGui.MenuItem("Reload Project Assemblies", _projectContext != null))
                    {
                        ReloadProjectAssemblies();
                    }
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Publish Project", _projectContext != null))
                    {
                        foreach (string option in _projectPublisher.PublishTargets)
                        {
                            if (ImGui.MenuItem(option))
                            {
                                _projectPublisher.PublishProject(
                                    _projectContext,
                                    option,
                                    Path.Combine(_projectContext.ProjectRootPath, $"Published/{option}"));
                            }
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Game"))
                {
                    if (ImGui.MenuItem("Play", "Ctrl+P", _playState == PlayState.Playing, _playState != PlayState.Playing && _currentScene != null)
                        || (ImGui.GetIO().KeysDown[(int)Key.P] && ImGui.GetIO().CtrlPressed) && _currentScene != null)
                    {
                        StartSimulation();
                    }
                    if (ImGui.MenuItem("Pause", "Ctrl+Shift+P", _playState == PlayState.Paused, _playState == PlayState.Playing))
                    {
                        PauseSimulation();
                    }
                    if (ImGui.MenuItem("Stop", "Ctrl+P", _playState == PlayState.Stopped, _playState != PlayState.Stopped))
                    {
                        StopSimulation();
                    }

                    ImGui.EndMenu();
                }

                if (_gs.Context.Window.WindowState == WindowState.FullScreen)
                {
                    float xStart = ImGui.GetWindowWidth() - ImGui.GetLastItemRectMax().X - 6;
                    ImGui.SameLine(0, xStart);
                    if (ImGui.Button("X"))
                    {
                        ExitEditor();
                    }
                }

                ImGui.EndMainMenuBar();
            }

            if (_currentScene != null && _input.GetKeyDown(Key.P) && (_input.GetKey(Key.ControlLeft) || _input.GetKey(Key.ControlRight)))
            {
                if (!_bus.Enabled)
                {
                    StartSimulation();
                }
                else if (_input.GetKey(Key.ShiftLeft) || _input.GetKey(Key.ShiftRight))
                {
                    PauseSimulation();
                }
                else
                {
                    StopSimulation();
                }
            }

            if (_playState == PlayState.Stopped && _input.GetKeyDown(Key.S) && (_input.GetKey(Key.ControlLeft) || _input.GetKey(Key.ControlRight)))
            {
                SaveCurrentScene(_currentScenePath);
            }

            if (openPopup != null)
            {
                ImGui.OpenPopup($"###{openPopup}");
            }

            if (ImGui.BeginPopup("###OpenProjectPopup"))
            {
                ImGui.Text("Path to project root:");
                if (openPopup != null)
                {
                    ImGuiNative.igSetKeyboardFocusHere(0);
                }
                if (ImGui.InputText(string.Empty, _filenameInputBuffer.Buffer, _filenameInputBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    LoadProject(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    LoadProject(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopup("###SaveSceneAsPopup"))
            {
                ImGui.Text("Destination Path:");
                if (openPopup != null)
                {
                    ImGuiNative.igSetKeyboardFocusHere(0);
                }
                if (ImGui.InputText(string.Empty, _filenameInputBuffer.Buffer, _filenameInputBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    LoadProject(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Save"))
                {
                    SaveCurrentScene(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void ReloadProjectAssemblies()
        {
            if (_sceneCam != null)
            {
                _sceneCam.Enabled = true;
            }
            SerializeGameObjectsToScene();
            DestroyNonEditorGameObjects();
            ClearProjectComponents();
            _als.CreateNewLoadContext();
            DiscoverProjectComponents();
            ActivateCurrentScene();
        }

        private void ClearProjectComponents()
        {
            _newComponentOptions.RemoveAll(_projectComponentsDiscovered.Contains);
            _as.Binder.ClearAssemblies();
            _projectComponentsDiscovered.Clear();
        }

        private GameObject CreateEmptyGameObject(Transform parent = null)
        {
            string prefix = "GameObject";
            int suffix = 0;
            while (_goQuery.FindByName(prefix + suffix) != null)
            {
                suffix += 1;
            }

            CreateGameObjectCommand c = new CreateGameObjectCommand(prefix + suffix, parent);
            _undoRedo.CommitCommand(c);
            return c.GameObject;
        }

        private bool LoadProject(string rootPathOrManifest)
        {
            if (File.Exists(rootPathOrManifest))
            {
                var loadedProjectRoot = new FileInfo(rootPathOrManifest).DirectoryName;
                _gs.Context.ResourceFactory.ShaderAssetRootPath = loadedProjectRoot;
                EditorPreferences.Instance.LastOpenedProjectRoot = rootPathOrManifest;
                var loadedProjectManifest = _as.ProjectDatabase.LoadAsset<ProjectManifest>(rootPathOrManifest);
                _as.ProjectAssetRootPath = Path.Combine(loadedProjectRoot, loadedProjectManifest.AssetRoot);
                _projectContext = new ProjectContext(loadedProjectRoot, loadedProjectManifest);
                DiscoverProjectComponents();
                return true;
            }
            else if (Directory.Exists(rootPathOrManifest))
            {
                string manifestPath = Path.Combine(rootPathOrManifest, NewProjectManifestName);
                var loadedProjectManifest = CreateNewManifest(manifestPath);
                var loadedProjectRoot = rootPathOrManifest;
                _gs.Context.ResourceFactory.ShaderAssetRootPath = rootPathOrManifest;
                EditorPreferences.Instance.LastOpenedProjectRoot = manifestPath;
                _as.ProjectDatabase.RootPath = Path.Combine(loadedProjectRoot, loadedProjectManifest.AssetRoot);
                _projectContext = new ProjectContext(loadedProjectRoot, loadedProjectManifest);
                return true;
            }

            StatusBarText("Couldn't load project from " + rootPathOrManifest, RgbaFloat.Red);
            return false;
        }

        private void DiscoverProjectComponents()
        {
            foreach (Assembly assembly in _als.LoadFromProjectManifest(_projectContext.ProjectManifest, _projectContext.ProjectRootPath))
            {
                foreach (Type discovered in DiscoverComponentsFromAssembly(assembly))
                {
                    _projectComponentsDiscovered.Add(discovered);
                }

                _as.Binder.AddProjectAssembly(assembly);
            }
        }

        private ProjectManifest CreateNewManifest(string manifestPath)
        {
            var manifest = new ProjectManifest()
            {
                Name = "NewProject"
            };
            using (var sw = File.CreateText(manifestPath))
            using (var jtw = new JsonTextWriter(sw))
            {
                _as.ProjectDatabase.DefaultSerializer.Serialize(jtw, manifest);
            }

            return manifest;
        }

        private void StatusBarText(string text) => StatusBarText(text, RgbaFloat.White);
        private void StatusBarText(string text, RgbaFloat color)
        {
            _statusBarText = text;
            _statusBarTextColor = color.ToVector4();
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

        private bool LoadScene(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            path = path.Trim(s_pathTrimChar);
            Console.WriteLine("Opening scene: " + path);

            try
            {
                SceneAsset loadedAsset;
                using (var fs = File.OpenRead(path))
                {
                    var jtr = new JsonTextReader(new StreamReader(fs));
                    loadedAsset = _as.ProjectDatabase.DefaultSerializer.Deserialize<SceneAsset>(jtr);
                }

                if (_currentScene == null)
                {
                    _currentScene = new InMemoryAsset<SceneAsset>();
                }
                _currentScene.UpdateAsset(_as.ProjectDatabase.DefaultSerializer, loadedAsset);
            }
            catch
            {
                return false;
            }

            StopSimulation();
            DestroyNonEditorGameObjects();
            ActivateCurrentScene();

            EditorPreferences.Instance.SetLatestScene(_projectContext.ProjectRootPath, path);
            _currentScenePath = path;

            return true;
        }

        private void ActivateScene(SceneAsset asset)
        {
            asset.GenerateGameObjects();
            _sceneCam = _gs.MainCamera;
            if (_sceneCam == _editorCamera)
            {
                Console.WriteLine("There was no camera in the scene.");
                _sceneCam = null;
            }

            if (_sceneCam != null)
            {
                _sceneCam.Enabled = false;
            }

            _editorCameraGO.Enabled = true;
            _gs.SetMainCamera(_editorCamera);
        }

        private void ActivateCurrentScene()
        {
            ActivateScene(_currentScene.GetAsset(_as.ProjectDatabase.DefaultSerializer));
        }

        private void SaveScene(SceneAsset scene, string path)
        {
            path = path.Trim(s_pathTrimChar);
            Console.WriteLine("Saving scene: " + path);
            using (var fs = File.CreateText(path))
            {
                var jtw = new JsonTextWriter(fs);
                _as.ProjectDatabase.DefaultSerializer.Serialize(jtw, scene);
            }
        }

        private void SaveCurrentScene(string path)
        {
            if (_sceneCam != null)
            {
                _sceneCam.Enabled = true;
            }

            SerializeGameObjectsToScene();
            SaveScene(_currentScene.GetAsset(_as.ProjectDatabase.DefaultSerializer), path);

            if (_sceneCam != null)
            {
                _sceneCam.Enabled = false;
                _gs.SetMainCamera(_editorCamera);
            }

            _currentScenePath = path;
        }

        private void SerializeGameObjectsToScene()
        {
            SerializedGameObject[] sGos = _goQuery.GetAllGameObjects().Where(go => !IsEditorObject(go))
                .Select(go => new SerializedGameObject(go)).ToArray();
            _currentScene.UpdateAsset(_as.ProjectDatabase.DefaultSerializer, new SceneAsset() { GameObjects = sGos });
        }

        private void DestroyNonEditorGameObjects()
        {
            foreach (var nonEditorGo in _goQuery.GetUnparentedGameObjects().Where(go => !IsEditorObject(go)))
            {
                nonEditorGo.Destroy();
            }

            _sceneCam = null;
        }

        private bool IsEditorObject(GameObject go)
        {
            return go.Name.StartsWith("__");
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
            Vector4 color = RgbaFloat.White.ToVector4();
            if (_selectedObjects.Contains(t.GameObject))
            {
                color = RgbaFloat.Cyan.ToVector4();
            }
            if (!t.GameObject.EnabledInHierarchy)
            {
                color = Vector4.Lerp(color, _disabledGrey, 0.5f);
            }
            ImGui.PushStyleColor(ColorTarget.Text, color);
            if (t.Children.Count > 0)
            {
                ImGui.SetNextTreeNodeOpened(true, SetCondition.FirstUseEver);
                bool opened = ImGui.TreeNode($"##{t.GameObject.Name}");
                if (_newSelectedObject == t.GameObject)
                {
                    _newSelectedObject = null;
                    ImGui.SetScrollHere();
                }
                ImGui.SameLine();
                if (ImGui.Selectable(t.GameObject.Name))
                {
                    GameObjectClicked(t.GameObject);
                }
                ImGui.PopStyleColor();
                if (ImGui.BeginPopupContextItem($"{t.GameObject.Name}_Context"))
                {
                    DrawContextMenuForGameObject(t.GameObject);
                }

                if (opened)
                {
                    foreach (var child in t.Children.ToArray())
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
                    GameObjectClicked(t.GameObject);
                }
                ImGui.PopStyleColor();

                if (_newSelectedObject == t.GameObject)
                {
                    _newSelectedObject = null;
                    ImGui.SetScrollHere();
                }

                if (ImGui.BeginPopupContextItem($"{t.GameObject.Name}_Context"))
                {
                    DrawContextMenuForGameObject(t.GameObject);
                }
            }
        }

        private void DrawContextMenuForGameObject(GameObject go)
        {
            if (ImGui.MenuItem("Focus Camera"))
            {
                ClearSelection();
                SelectObject(go);
                MoveCameraTo(go.Transform);
            }
            if (ImGui.MenuItem("Enabled", string.Empty, go.Enabled, true))
            {
                go.Enabled = !go.Enabled;
            }
            if (ImGui.MenuItem("Clone", string.Empty))
            {
                CloneGameObject(go, go.Transform.Parent);
            }
            if (ImGui.MenuItem("Delete", string.Empty))
            {
                DeleteGameObject(go);
            }
            ImGui.EndPopup();
        }

        private void MoveCameraTo(Transform t)
        {
            _editorCameraGO.Transform.Position = t.Position - _editorCameraGO.Transform.Forward * 5.0f;
        }

        private void DeleteGameObjects(IEnumerable<GameObject> gos)
        {
            foreach (var go in gos)
            {
                DeleteGameObject(go);
            }
        }

        private static void DeleteGameObject(GameObject go)
        {
            go.Destroy();
        }

        private void CloneGameObject(GameObject go, Transform parent)
        {
            SerializedGameObject sgo = new SerializedGameObject(go);
            using (var fs = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096))
            {
                var textWriter = new JsonTextWriter(new StreamWriter(fs));
                _as.ProjectDatabase.DefaultSerializer.Serialize(textWriter, sgo);
                textWriter.Flush();
                fs.Seek(0, SeekOrigin.Begin);
                var reader = new JsonTextReader(new StreamReader(fs));
                sgo = _as.ProjectDatabase.DefaultSerializer.Deserialize<SerializedGameObject>(reader);
            }

            GameObject newGo = new GameObject($"{go.Name} (Clone)");
            newGo.Transform.LocalPosition = sgo.Transform.LocalPosition;
            newGo.Transform.LocalRotation = sgo.Transform.LocalRotation;
            newGo.Transform.LocalScale = sgo.Transform.LocalScale;

            foreach (var comp in sgo.Components)
            {
                newGo.AddComponent(comp);
            }

            if (parent != null)
            {
                newGo.Transform.Parent = parent;
            }

            foreach (var child in go.Transform.Children)
            {
                CloneGameObject(child.GameObject, newGo.Transform);
            }

            ClearSelection();
            SelectObject(newGo);
        }

        private void SelectObject(GameObject go)
        {
            _selectedObjects.Add(go);
            go.Destroyed += OnSelectedDestroyed;
            var mrs = go.GetComponents<MeshRenderer>();
            foreach (var mr in mrs)
            {
                mr.Tint = new TintInfo(new Vector3(1.0f), 0.6f);
            }

            _newSelectedObject = go;
        }

        private void Deselect(GameObject go)
        {
            UntintAndUnsubscribe(go);
            _selectedObjects.Remove(go);
        }

        private void ClearSelection()
        {
            if (_selectedObjects.Any())
            {
                foreach (var go in _selectedObjects)
                {
                    UntintAndUnsubscribe(go);
                }

                _selectedObjects.Clear();
            }

            _selectedAsset = null;
            _loadedAssetPath = null;
        }

        private void UntintAndUnsubscribe(GameObject go)
        {
            go.Destroyed -= OnSelectedDestroyed;
            var mrs = go.GetComponents<MeshRenderer>();
            foreach (var mr in mrs)
            {
                mr.Tint = new TintInfo();
            }
        }

        void OnSelectedDestroyed(GameObject go)
        {
            Debug.Assert(_selectedObjects.Contains(go));
            _selectedObjects.Remove(go);
        }

        private void MultiDrawObjects(ICollection<GameObject> gos)
        {
            if (ImGui.CollapsingHeader($"Editing {gos.Count} selected GameObjects", "MultiDraw", true, true))
            {
                MultiDrawTransform(gos);

                var componentGroups = gos.SelectMany(go => go.GetComponents<Component>())
                    .Where(c => c.GetType() != typeof(Transform))
                    .GroupBy(c => c.GetType()).Where(group => group.Count() == gos.Count);
                ImGui.Text("Shared Components:");
                foreach (var group in componentGroups)
                {
                    MultiDrawComponentGroup(group);
                }
            }
        }

        private void MultiDrawTransform(ICollection<GameObject> gos)
        {
            Command c = null;
            Transform t = _multiTransformDummy;
            Vector3 startPos = t.LocalPosition;
            Vector3 startScale = t.LocalScale;
            Quaternion startRotation = t.LocalRotation;

            Vector3 pos = t.LocalPosition;
            if (ImGui.DragVector3("Position", ref pos, -50f, 50f, 0.05f, "multi"))
            {
                t.LocalPosition = pos;
                c = new CompoundCommand(gos.Select(go => go.Transform)
                    .Select(transform => SetValueActionCommand.New<Vector3>(
                        val => transform.LocalPosition = val, transform.LocalPosition, transform.LocalPosition + pos - startPos))
                    .ToArray());
            }

            object rotation = t.LocalRotation;
            var drawer = DrawerCache.GetDrawer(typeof(Quaternion));
            if (drawer.Draw("Rotation", ref rotation, null))
            {
                t.LocalRotation = (Quaternion)rotation;
                c = new CompoundCommand(gos.Select(go => go.Transform)
                    .Select(transform => SetValueActionCommand.New<Quaternion>(
                        val => transform.LocalRotation = val, transform.LocalRotation, transform.LocalRotation + (Quaternion)rotation - startRotation))
                    .ToArray());
            }

            float scale = t.LocalScale.X;
            if (ImGui.DragFloat("Scale", ref scale, .01f, 50f, 0.05f))
            {
                t.LocalScale = new Vector3(scale);
                c = new CompoundCommand(gos.Select(go => go.Transform)
                    .Select(transform => SetValueActionCommand.New<float>(
                        val => transform.LocalScale = new Vector3(val), transform.LocalScale.X, transform.LocalScale.X + scale - startScale.X))
                    .ToArray());
            }

            if (c != null)
            {
                _undoRedo.CommitCommand(c);
            }
        }

        private void MultiDrawComponentGroup(IGrouping<Type, Component> group)
        {
        }

        private void DrawSingleObject(GameObject go)
        {
            ImGui.PushStyleVar(StyleVar.FramePadding, new Vector2());
            if (ImGui.BeginChildFrame((uint)"GoHeader".GetHashCode(), new Vector2(0, 25), WindowFlags.ShowBorders))
            {
                bool enabled = go.Enabled;
                if (ImGui.Checkbox("###GameObjectEnabled", ref enabled))
                {
                    go.Enabled = enabled;
                }
                ImGui.SameLine(0, 5);
                _goNameBuffer.StringValue = go.Name;
                if (ImGui.InputText("###GoNameInput", _goNameBuffer.Buffer, _goNameBuffer.Length, InputTextFlags.Default, null))
                {
                    go.Name = _goNameBuffer.ToString();
                }

                ImGui.EndChildFrame();
            }
            ImGui.PopStyleVar();

            int id = 0;
            foreach (var component in go.GetComponents<Component>())
            {
                ImGui.PushID(id++);
                Command c = DrawComponent(component);
                if (c != null)
                {
                    _undoRedo.CommitCommand(c);
                }
                ImGui.PopID();
            }

            DrawNewComponentAdder(go);
        }

        private void DrawNewComponentAdder(GameObject go)
        {
            ImGui.PushStyleColor(ColorTarget.Button, RgbaFloat.Red.ToVector4());
            if (ImGui.Button("Add New Component"))
            {
                ImGui.OpenPopup("###NewComponentAdder");
            }
            ImGui.PopStyleColor();

            if (ImGui.BeginPopup("###NewComponentAdder"))
            {
                foreach (Type option in _newComponentOptions)
                {
                    if (ImGui.MenuItem(option.Name))
                    {
                        Component c = (Component)Activator.CreateInstance(option);
                        Command command = new RawCommand(() => go.AddComponent(c), () => go.RemoveComponent(c));
                        _undoRedo.CommitCommand(command);
                    }
                }

                ImGui.EndPopup();
            }
        }

        private Command DrawComponent(Component component)
        {
            Command c = null;

            var type = component.GetType();
            var drawer = EditorDrawerCache.GetDrawer(type);
            object componentAsObject = component;
            Vector4 color = RgbaFloat.CornflowerBlue.ToVector4();
            if (!component.EnabledInHierarchy)
            {
                color = Vector4.Lerp(color, _disabledGrey, 0.85f);
            }
            ImGui.PushStyleColor(ColorTarget.Header, color);
            if (ImGui.CollapsingHeader(type.Name, type.Name, true, true))
            {
                if (ImGui.BeginPopupContextItem(type.Name + "_Context"))
                {
                    if (ImGui.MenuItem("Enabled", string.Empty, component.Enabled, true))
                    {
                        component.Enabled = !component.Enabled;
                    }
                    if (ImGui.MenuItem("Remove"))
                    {
                        var go = component.GameObject;
                        Command command = new RawCommand(() => go.RemoveComponent(component), () => go.AddComponent(component));
                        _undoRedo.CommitCommand(command);
                    }
                    ImGui.EndPopup();
                }
                c = drawer.Draw(type.Name, componentAsObject, _gs.Context);
            }
            else
            {
                if (ImGui.BeginPopupContextItem(type.Name + "_Context"))
                {
                    if (ImGui.MenuItem("Remove"))
                    {
                        var go = component.GameObject;
                        Command command = new RawCommand(() => go.RemoveComponent(component), () => go.AddComponent(component));
                        _undoRedo.CommitCommand(command);
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.PopStyleColor();

            return c;
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

    public enum PlayState
    {
        Stopped,
        Paused,
        Playing
    }
}
