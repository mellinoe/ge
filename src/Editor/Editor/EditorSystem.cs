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
using Engine.Audio;
using Veldrid.Graphics.Direct3D;
using SharpDX.Direct3D11;
using SharpFont;
using Veldrid;
using System.Runtime.InteropServices;

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
        private readonly EditorSceneLoaderSystem _sls;
        private readonly CommandLineOptions _commandLineOptions;

        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<EditorBehavior> _newStarts = new List<EditorBehavior>();

        private readonly TextInputBuffer _filenameInputBuffer = new TextInputBuffer(256);
        private readonly TextInputBuffer _assetFileNameBufer = new TextInputBuffer(100);
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

        private InMemoryAsset<SceneAsset> _currentScene = new InMemoryAsset<SceneAsset>();
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
        private InMemoryAsset<Component> _componentCopySource;
        private string _componentCopySourceType;
        private GameObject _newSelectedObject;
        private List<RayCastHit<RenderItem>> _gsRCHits = new List<RayCastHit<RenderItem>>();
        private bool _focusNameField;
        private static readonly Type s_transformType = typeof(Transform);

        public EditorSystem(SystemRegistry registry, CommandLineOptions commandLineOptions, ImGuiRenderer imGuiRenderer)
        {
            _registry = registry;
            _physics = registry.GetSystem<PhysicsSystem>();
            _input = registry.GetSystem<InputSystem>();
            _goQuery = registry.GetSystem<GameObjectQuerySystem>();
            _gs = registry.GetSystem<GraphicsSystem>();
            _as = (EditorAssetSystem)registry.GetSystem<AssetSystem>();
            _bus = registry.GetSystem<BehaviorUpdateSystem>();
            _als = registry.GetSystem<AssemblyLoadSystem>();
            _sls = (EditorSceneLoaderSystem)registry.GetSystem<SceneLoaderSystem>();
            _commandLineOptions = commandLineOptions;

            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<Transform>(DrawTransform));
            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<MeshRenderer>(DrawMeshRenderer));
            EditorDrawerCache.AddDrawer(new FuncEditorDrawer<Component>(GenericDrawer));

            DrawerCache.AddDrawer(new FuncDrawer<RefOrImmediate<TextureData>>(DrawTextureRef));
            DrawerCache.AddDrawer(new FuncDrawer<RefOrImmediate<MeshData>>(DrawMeshRef));
            DrawerCache.AddDrawer(new FuncDrawer<AssetRef<SceneAsset>>(DrawSceneRef));
            DrawerCache.AddDrawer(new FuncDrawer<AssetRef<WaveFile>>(DrawWaveRef));
            DrawerCache.AddDrawer(new FuncDrawer<AssetRef<FontFace>>(DrawFontRef));
            DrawerCache.AddDrawer(new FuncDrawer<PhysicsLayersDescription>(PhysicsLayersDrawer));

            var genericHandler = new GenericAssetMenuHandler(); _assetMenuHandlers.AddItem(genericHandler.TypeHandled, genericHandler);
            var sceneHandler = new ExplicitMenuHandler<SceneAsset>(() => { }, (path) => LoadScene(path));
            _assetMenuHandlers.AddItem(sceneHandler.TypeHandled, sceneHandler);

            var prefabHandler = new PrefabAssetHandler(_goQuery, this);
            _assetMenuHandlers.AddItem(prefabHandler.TypeHandled, prefabHandler);

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

            _bus.Remove(imGuiRenderer);
            RegisterBehavior(imGuiRenderer);

            _axesRenderer = new AxesRenderer(_gs.Context, _gs);
            _gs.AddFreeRenderItem(_axesRenderer);

            DiscoverComponentsFromAssembly(typeof(Game).GetTypeInfo().Assembly);

            string projectToLoad = commandLineOptions.Project ?? EditorPreferences.Instance.LastOpenedProjectRoot;
            if (!string.IsNullOrEmpty(projectToLoad))
            {
                if (LoadProject(projectToLoad))
                {
                    var latestScene = commandLineOptions.Scene
                        ?? EditorPreferences.Instance.GetLastOpenedScene(_projectContext.ProjectRootPath);
                    if (!string.IsNullOrEmpty(latestScene))
                    {
                        try
                        {
                            LoadScene(latestScene);
                        }
                        catch
                        {
                            StatusBarText("[!] An error was encountered when loading " + latestScene, RgbaFloat.Red);
                            EditorPreferences.Instance.SetLatestScene(_projectContext.ProjectRootPath, string.Empty);
                            CloseScene();
                        }
                    }
                }
            }
        }

        private TextInputBuffer _physicsLayerInput = new TextInputBuffer(128);
        private bool PhysicsLayersDrawer(string label, ref PhysicsLayersDescription pld, RenderContext rc)
        {
            for (int i = 0; i < pld.GetLayerCount(); i++)
            {
                _physicsLayerInput.StringValue = pld.GetLayerName(i);
                if (ImGui.InputText($"[{i}]", _physicsLayerInput.Buffer, _physicsLayerInput.Length, InputTextFlags.Default, null))
                {
                    pld.SetLayerName(i, _physicsLayerInput.StringValue);
                }

                for (int g = 0; g < pld.GetLayerCount(); g++)
                {
                    bool colliding = pld.GetDoLayersCollide(i, g);
                    ImGui.SameLine();
                    if (ImGui.Checkbox($"##C{i}{g}", ref colliding))
                    {
                        pld.SetLayersCollide(i, g, colliding);
                    }
                    if (ImGui.IsLastItemHovered())
                    {
                        ImGui.SetTooltip($"{pld.GetLayerName(i)} <-> {pld.GetLayerName(g)}");
                    }
                }
            }

            if (ImGui.Button("New Layer"))
            {
                pld.AddLayer("New Layer");
            }
            if (pld.GetLayerCount() > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button("Remove Last"))
                {
                    pld.RemoveLastLayer();
                }
            }

            return false;
        }

        private bool DrawSceneRef(string label, ref AssetRef<SceneAsset> sceneRef, RenderContext rc)
        {
            return DrawAssetRef(label, ref sceneRef, _as.Database);
        }

        private bool DrawWaveRef(string label, ref AssetRef<WaveFile> waveRef, RenderContext rc)
        {
            return DrawAssetRef(label, ref waveRef, _as.Database);
        }

        private bool DrawFontRef(string label, ref AssetRef<FontFace> fontRef, RenderContext rc)
        {
            return DrawAssetRef(label, ref fontRef, _as.Database);
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
                _editorCameraGO.Enabled = false;
                if (_sceneCam != null)
                {
                    _sceneCam.Enabled = true;
                }
                else
                {
                    Console.WriteLine("No camera in the current scene.");
                }

                if (_playState == PlayState.Stopped)
                {
                    SerializeGameObjectsToScene(_currentScene);
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
                }

                _editorCameraGO.Enabled = true;
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

            bool castShadows = mr.CastShadows;
            if (ImGui.Checkbox("Cast Shadows", ref castShadows))
            {
                c = SetValueActionCommand.New<bool>(val => mr.CastShadows = val, mr.CastShadows, castShadows);
            }

            if (!mr.Mesh.HasValue)
            {
                AssetRef<MeshData> assetRef = mr.Mesh.GetRef();
                if (DrawAssetRef("Model", ref assetRef, _as.Database))
                {
                    c = SetValueActionCommand.New<AssetRef<MeshData>>(val => mr.Mesh = val, mr.Mesh.GetRef(), assetRef);
                }
            }

            if (!mr.Texture.HasValue)
            {
                AssetRef<TextureData> assetRef = mr.Texture.GetRef();
                if (DrawAssetRef("Texture", ref assetRef, _as.Database))
                {
                    c = SetValueActionCommand.New<AssetRef<TextureData>>(val => mr.Texture = val, mr.Texture.GetRef(), assetRef);
                }
            }

            Vector3 color = mr.BaseTint.Color;
            if (ImGui.ColorEdit3("Tint Color", ref color, false))
            {
                c = SetValueActionCommand.New<TintInfo>(val => mr.BaseTint = val, mr.BaseTint, new TintInfo(color, mr.BaseTint.TintFactor));
            }

            float tintFactor = mr.BaseTint.TintFactor;
            if (ImGui.DragFloat("Tint Factor", ref tintFactor, 0f, 1f, 0.05f))
            {
                c = SetValueActionCommand.New<TintInfo>(val => mr.BaseTint = val, mr.BaseTint, new TintInfo(mr.BaseTint.Color, tintFactor));
            }

            float opacity = mr.Opacity;
            if (ImGui.DragFloat("Opacity", ref opacity, 0f, 1f, 0.05f))
            {
                c = SetValueActionCommand.New<float>(val => mr.Opacity = val, mr.Opacity, opacity);
            }

            if (ImGui.Button("Toggle Bounds Renderer"))
            {
                mr.ToggleBoundsRenderer();
            }

            return c;
        }

        private bool DrawTextureRef(string label, ref RefOrImmediate<TextureData> obj, RenderContext rc)
        {
            AssetRef<TextureData> meshRef = obj.GetRef() ?? new AssetRef<TextureData>();
            if (DrawAssetRef(label, ref meshRef, _as.Database))
            {
                obj = new RefOrImmediate<TextureData>(new AssetRef<TextureData>(meshRef.ID), null);
                return true;
            }

            return false;
        }

        private bool DrawMeshRef(string label, ref RefOrImmediate<MeshData> obj, RenderContext rc)
        {
            AssetRef<MeshData> meshRef = obj.GetRef() ?? new AssetRef<MeshData>();
            if (DrawAssetRef(label, ref meshRef, _as.Database))
            {
                obj = new RefOrImmediate<MeshData>(new AssetRef<MeshData>(meshRef.ID), null);
                return true;
            }

            return false;
        }

        private static bool DrawAssetRef<T>(string label, ref AssetRef<T> existingRef, AssetDatabase database)
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
                existingRef = new AssetRef<T>(result);
                return true;
            }
            else
            {
                return false;
            }
        }

        private Command DrawTransform(string label, Transform t, RenderContext rc)
        {
            Command c = null;

            Vector3 pos = t.LocalPosition;
            if (ImGui.DragVector3("Position", ref pos, -10000f, 10000f, 0.05f))
            {
                c = SetValueActionCommand.New<Vector3>((val) => t.LocalPosition = val, t.LocalPosition, pos);
            }

            object rotation = t.LocalRotation;
            var drawer = DrawerCache.GetDrawer(typeof(Quaternion));
            if (drawer.Draw("Rotation", ref rotation, null))
            {
                c = SetValueActionCommand.New<Quaternion>((val) => t.LocalRotation = val, t.LocalRotation, rotation);
            }

            if (_uniformScaleTool)
            {
                float scale = t.LocalScale.X;
                if (ImGui.DragFloat("##ScaleDrag", ref scale, .01f, 10000f, 0.05f))
                {
                    c = SetValueActionCommand.New<Vector3>((val) => t.LocalScale = val, t.LocalScale, new Vector3(scale));
                }
                ImGui.SameLine();
                if (ImGui.Button("Scale"))
                {
                    _uniformScaleTool = !_uniformScaleTool;
                }
            }
            else
            {
                Vector3 scale = t.LocalScale;
                if (ImGui.DragVector3("##ScaleDrag", ref scale, .01f, 10000f, 0.05f))
                {
                    c = SetValueActionCommand.New<Vector3>((val) => t.LocalScale = val, t.LocalScale, scale);
                }
                ImGui.SameLine();
                if (ImGui.Button("Scale"))
                {
                    _uniformScaleTool = !_uniformScaleTool;
                }
            }

            return c;
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds * 1000.0);
            if (_gs.Context.Window.Exists)
            {
                _gs.Context.Window.Title = $"ge.Editor " + _fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTime.ToString("#00.00 ms");
            }
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

                    int hits = _gs.RayCast(ray, _gsRCHits);
                    if (hits > 0)
                    {
                        var last = (Component)_gsRCHits.OrderBy(hit => hit.Distance).FirstOrDefault(hit => hit.Item is Component).Item;
                        if (last != null)
                        {
                            GameObject go = last.GameObject;
                            GameObjectClicked(go);
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
                bool save = false;
                if (_focusNameField)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusNameField = false;
                }
                if (ImGui.InputText("###AssetNameInput", _assetFileNameBufer.Buffer, _assetFileNameBufer.Length, InputTextFlags.EnterReturnsTrue, null))
                {
                    save = true;
                }
                ImGui.PopItemWidth();
                ImGui.SameLine();
                if (ImGui.Button("Save"))
                {
                    save = true;
                }

                if (save)
                {
                    FileInfo fi = new FileInfo(_loadedAssetPath);
                    _as.ProjectDatabase.SaveDefinition(_selectedAsset, _loadedAssetPath);
                    string newPath = Path.Combine(fi.Directory.FullName, _assetFileNameBufer.StringValue);
                    File.Move(_loadedAssetPath, newPath);
                    _loadedAssetPath = newPath;
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

        private DirectoryNode _projectRootDirectoryNode;
        private TimeSpan _rootNodeRefreshPeriod = TimeSpan.FromSeconds(1);
        private DateTime _lastRootNodeRefreshTime = DateTime.MinValue;
        private string _currentSceneName;
        private readonly List<ColliderShapeRenderer> _cachedColliderRenderers = new List<ColliderShapeRenderer>();
        private readonly Dictionary<GameObject, ColliderShapeRenderer> _colliderRenderers = new Dictionary<GameObject, ColliderShapeRenderer>();
        private bool _uniformScaleTool = true;
        private float _numPadMoveSensitivity = 0.5f;

        private void DrawProjectAssets()
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastRootNodeRefreshTime > _rootNodeRefreshPeriod)
            {
                _projectRootDirectoryNode = _as.ProjectDatabase.GetRootDirectoryGraph();
                _lastRootNodeRefreshTime = now;
            }

            if (!string.IsNullOrEmpty(_projectContext?.ProjectRootPath))
            {
                DrawRecursiveNode(_projectRootDirectoryNode, false);
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
                        _assetFileNameBufer.StringValue = asset.Name;
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

                        handler.DrawMenuItems(() => _as.Database.LoadAsset(asset.Path));

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
                    if (ImGui.MenuItem("Save Scene", "Ctrl+S", false, !string.IsNullOrEmpty(_currentScenePath) && _playState == PlayState.Stopped))
                    {
                        SaveCurrentScene(_currentScenePath);
                    }
                    if (ImGui.MenuItem("Save Scene As", "Ctrl+S", false, _playState == PlayState.Stopped))
                    {
                        openPopup = "SaveSceneAsPopup";
                    }
                    if (ImGui.MenuItem("Close Scene", string.Empty, false, _currentScene != null))
                    {
                        CloseScene();
                    }
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Publish", _projectContext != null))
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
                    if (ImGui.MenuItem("Freeze Debug Line Rendering"))
                    {
                        _gs.ToggleFreezeLines();
                    }
                    float renderQuality = _gs.RenderQuality;
                    if (ImGui.DragFloat("Render Quality", ref renderQuality, 0.1f, 1f, 0.01f))
                    {
                        _gs.RenderQuality = renderQuality;
                        EditorPreferences.Instance.RenderQuality = renderQuality;
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
                        _focusNameField = true;
                    }
                    if (ImGui.MenuItem("Create Empty Child", _selectedObjects.Any()))
                    {
                        var newChild = CreateEmptyGameObject(_selectedObjects.First().Transform);
                        ClearSelection();
                        SelectObject(newChild);
                        _focusNameField = true;
                    }
                    if (ImGui.MenuItem("Create Empty Parent", _selectedObjects.Any()))
                    {
                        Vector3 position = GetSelectionCenter();
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
                        _focusNameField = true;
                    }
                    if (ImGui.MenuItem("Create Prefab From Selected", _selectedObjects.Count == 1))
                    {
                        _selectedAsset = CreateGameObjectPrefab(_selectedObjects.Single(), out _loadedAssetPath);
                        _focusNameField = true;
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Unparent Selected", _selectedObjects.Any(go => go.Transform.Parent != null)))
                    {
                        foreach (var selected in _selectedObjects.Where(go => go.Transform.Parent != null))
                        {
                            selected.Transform.Parent = selected.Transform.Parent.Parent;
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
                    if (ImGui.MenuItem("Edit Project Manifest"))
                    {
                        openPopup = "EditProjectManifestPopup";
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Reload Project Assemblies", _projectContext != null))
                    {
                        ReloadProjectAssemblies();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Game"))
                {
                    if (ImGui.MenuItem("Play", "Ctrl+P", _playState == PlayState.Playing, _playState != PlayState.Playing)
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
                if (ImGui.BeginMenu("Debug"))
                {
                    if (ImGui.MenuItem("Debugger Break", Debugger.IsAttached))
                    {
                        Debugger.Break();
                    }
                    if (ImGui.BeginMenu("Direct3D", _gs.Context is D3DRenderContext))
                    {
                        if (ImGui.MenuItem("Report Live Objects (Summary)"))
                        {
                            ((D3DRenderContext)_gs.Context).Device.QueryInterface<DeviceDebug>().ReportLiveDeviceObjects(ReportingLevel.Summary);
                        }
                        if (ImGui.MenuItem("Report Live Objects (Detailed)"))
                        {
                            ((D3DRenderContext)_gs.Context).Device.QueryInterface<DeviceDebug>().ReportLiveDeviceObjects(ReportingLevel.Detail);
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }

                if (_gs.Context.Window.Exists)
                {
                    WindowState currentWindowState = _gs.Context.Window.WindowState;
                    if (currentWindowState == WindowState.FullScreen || currentWindowState == WindowState.BorderlessFullScreen)
                    {
                        float xStart = ImGui.GetWindowWidth() - ImGui.GetLastItemRectMax().X - 6;
                        ImGui.SameLine(0, xStart);
                        if (ImGui.Button("X"))
                        {
                            ExitEditor();
                        }
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

            if (_selectedObjects.Any() && _input.GetKeyDown(Key.C) && _input.GetKey(Key.ShiftLeft) && _input.GetKey(Key.ControlLeft))
            {
                GameObject source = _selectedObjects.First();
                CloneGameObject(source, source.Transform.Parent);
                _focusNameField = true;
            }

            if (_playState == PlayState.Stopped && !string.IsNullOrEmpty(_currentScenePath) && _input.GetKeyDown(Key.S) && (_input.GetKey(Key.ControlLeft) || _input.GetKey(Key.ControlRight)))
            {
                SaveCurrentScene(_currentScenePath);
            }

            if (_selectedObjects.Any() && !ImGui.IsAnyItemHovered())
            {
                HandleNumPadMovement();
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
                    ImGui.SetKeyboardFocusHere();
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
                    ImGui.SetKeyboardFocusHere();
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

            if (ImGui.BeginPopup("###EditProjectManifestPopup"))
            {
                Drawer d = DrawerCache.GetDrawer(typeof(ProjectManifest));
                object manifest = _projectContext.ProjectManifest;
                if (d.Draw("Project Manifest", ref manifest, _gs.Context))
                {
                }

                if (ImGui.Button("Save"))
                {
                    _as.ProjectDatabase.SaveDefinition(manifest, _projectContext.ProjectManifestPath);
                }

                ImGui.EndPopup();
            }
        }

        private Vector3 GetSelectionCenter()
        {
            return MathUtil.SumAll(_selectedObjects.Select(go => go.Transform.Position)) / _selectedObjects.Count;
        }

        private void HandleNumPadMovement()
        {
            if (_input.GetKeyDown(Key.Keypad5))
            {
                Vector3 center = GetSelectionCenter();
                MoveCameraTo(center);
            }

            var keyPresses = _input.CurrentSnapshot.KeyCharPresses;
            Vector3 direction = Vector3.Zero;
            if (_input.GetKey(Key.Keypad8) && keyPresses.Contains('8'))
            {
                direction = -Vector3.UnitZ;
            }
            if (_input.GetKey(Key.Keypad4) && keyPresses.Contains('4'))
            {
                direction = -Vector3.UnitX;
            }
            if (_input.GetKey(Key.Keypad6) && keyPresses.Contains('6'))
            {
                direction = Vector3.UnitX;
            }
            if (_input.GetKey(Key.Keypad2) && keyPresses.Contains('2'))
            {
                direction = Vector3.UnitZ;
            }
            if (_input.GetKey(Key.Keypad7) && keyPresses.Contains('7'))
            {
                direction = -Vector3.UnitY;
            }
            if (_input.GetKey(Key.Keypad9) && keyPresses.Contains('9'))
            {
                direction = Vector3.UnitY;
            }

            if (direction != Vector3.Zero)
            {
                Command c = new CompoundCommand(
                    _selectedObjects.Select(go => SetValueActionCommand.New<Vector3>(
                        v => go.Transform.Position = v,
                        go.Transform.Position,
                        go.Transform.Position + direction * _numPadMoveSensitivity)).ToArray());

                _undoRedo.CommitCommand(c);
            }
        }

        private void CloseScene()
        {
            StopSimulation();
            DestroyNonEditorGameObjects();
            _currentScenePath = string.Empty;
        }

        private SerializedPrefab CreateGameObjectPrefab(GameObject go, out string assetPath)
        {
            List<GameObject> allChildren = new List<GameObject>();
            CollectChildren(go.Transform, allChildren);
            SerializedPrefab sp = new SerializedPrefab(allChildren);
            assetPath = _as.ProjectDatabase.SaveDefinition(sp, $"{go.Name}.prefab");
            return sp;
        }

        private void CollectChildren(Transform t, List<GameObject> allChildren)
        {
            allChildren.Add(t.GameObject);
            foreach (Transform child in t.Children)
            {
                CollectChildren(child, allChildren);
            }
        }

        private void ReloadProjectAssemblies()
        {
            if (_sceneCam != null)
            {
                _sceneCam.Enabled = true;
            }

            InMemoryAsset<SceneAsset> tempScene = new InMemoryAsset<SceneAsset>();
            SerializeGameObjectsToScene(tempScene);
            DestroyNonEditorGameObjects();
            ClearProjectComponents();
            _als.CreateNewLoadContext();
            DiscoverProjectComponents();
            ActivateScene(tempScene.GetAsset(_as.ProjectDatabase.DefaultSerializer));

            if (_playState == PlayState.Playing)
            {
                _editorCameraGO.Enabled = false;
                if (_sceneCam != null)
                {
                    _sceneCam.Enabled = true;
                }
            }
        }

        private void ClearProjectComponents()
        {
            _newComponentOptions.RemoveAll(_projectComponentsDiscovered.Contains);
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
                ClearProjectComponents();
                _projectContext = new ProjectContext(loadedProjectRoot, loadedProjectManifest, rootPathOrManifest);
                DiscoverProjectComponents();
                _physics.SetPhysicsLayerRules(loadedProjectManifest.PhysicsLayers);
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
                _projectContext = new ProjectContext(loadedProjectRoot, loadedProjectManifest, manifestPath);
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
            }
        }

        private ProjectManifest CreateNewManifest(string manifestPath)
        {
            var manifest = new ProjectManifest()
            {
                Name = "NewProject",
                PhysicsLayers = PhysicsLayersDescription.Default
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
            WindowState maximizedState = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowState.BorderlessFullScreen : WindowState.FullScreen; 
            window.WindowState = state != maximizedState ? maximizedState : WindowState.Normal;
        }

        private void ExitEditor()
        {
            _gs.Context.Window.Close();
        }

        private bool LoadScene(string path)
        {
            path = path.Trim(s_pathTrimChar);
            Console.WriteLine("Opening scene: " + path);

            SceneAsset loadedAsset;
            if (!_as.ProjectDatabase.TryLoadAsset(path, false, out loadedAsset))
            {
                return false;
            }
            _currentSceneName = loadedAsset.Name;

            if (_currentScene == null)
            {
                _currentScene = new InMemoryAsset<SceneAsset>();
            }
            _currentScene.UpdateAsset(_as.ProjectDatabase.DefaultSerializer, loadedAsset);

            ClearSelection();
            bool wasPlaying = _playState == PlayState.Playing;
            StopSimulation();
            _sceneCam = null;
            _sls.LoadScene(loadedAsset, delayTilEndOfFrame: wasPlaying);
            RefreshCameras();

            EditorPreferences.Instance.SetLatestScene(_projectContext.ProjectRootPath, path);
            _currentScenePath = path;

            return true;
        }

        private void ActivateCurrentScene()
        {
            ActivateScene(_currentScene.GetAsset(_as.ProjectDatabase.DefaultSerializer));
        }

        private void ActivateScene(SceneAsset asset)
        {
            asset.GenerateGameObjects();
            RefreshCameras();
        }

        private void RefreshCameras()
        {
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

        private void SaveScene(SceneAsset scene, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Invalid path.");
            }

            Console.WriteLine("Saving scene: " + path);
            _as.ProjectDatabase.SaveDefinition(scene, path);

            StatusBarText($"[{DateTime.Now.ToString()}] Saved scene to {path}.", RgbaFloat.Cyan);
        }

        private void SaveCurrentScene(string path)
        {
            if (_sceneCam != null)
            {
                _sceneCam.Enabled = true;
            }

            SerializeGameObjectsToScene(_currentScene);
            SaveScene(_currentScene.GetAsset(_as.ProjectDatabase.DefaultSerializer), path);

            if (_sceneCam != null)
            {
                _sceneCam.Enabled = false;
                _gs.SetMainCamera(_editorCamera);
            }

            _currentScenePath = path;
        }

        private void SerializeGameObjectsToScene(InMemoryAsset<SceneAsset> asset)
        {
            SerializedGameObject[] sGos = _goQuery.GetAllGameObjects().Where(go => !EditorUtility.IsEditorObject(go))
                .Select(go => new SerializedGameObject(go)).ToArray();
            asset.UpdateAsset(_as.ProjectDatabase.DefaultSerializer, new SceneAsset() { GameObjects = sGos, Name = _currentSceneName });
        }

        private void DestroyNonEditorGameObjects()
        {
            foreach (var nonEditorGo in _goQuery.GetUnparentedGameObjects().Where(go => !EditorUtility.IsEditorObject(go)))
            {
                nonEditorGo.Destroy();
            }

            _sceneCam = null;
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
                ImGui.SetNextTreeNodeOpen(true, SetCondition.FirstUseEver);
                bool opened = ImGui.TreeNode($"##{t.GameObject.ID}");
                if (_newSelectedObject == t.GameObject)
                {
                    _newSelectedObject = null;
                    ImGui.SetScrollHere();
                }
                ImGui.SameLine();
                if (ImGui.Selectable($"{t.GameObject.Name}##{t.GameObject.ID}"))
                {
                    GameObjectClicked(t.GameObject);
                }
                ImGui.PopStyleColor();
                if (ImGui.BeginPopupContextItem($"{t.GameObject.ID}_Context"))
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
                if (ImGui.Selectable($"{t.GameObject.Name}##{t.GameObject.ID}"))
                {
                    GameObjectClicked(t.GameObject);
                }
                ImGui.PopStyleColor();

                if (_newSelectedObject == t.GameObject)
                {
                    _newSelectedObject = null;
                    ImGui.SetScrollHere();
                }

                if (ImGui.BeginPopupContextItem($"{t.GameObject.ID}_Context"))
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
                MoveCameraTo(go.Transform.Position);
            }
            if (ImGui.MenuItem("Enabled", string.Empty, go.Enabled, true))
            {
                go.Enabled = !go.Enabled;
            }
            if (ImGui.MenuItem("Clone", string.Empty))
            {
                CloneGameObject(go, go.Transform.Parent);
                _focusNameField = true;
            }
            if (ImGui.MenuItem("Delete", string.Empty))
            {
                DeleteGameObject(go);
            }
            ImGui.EndPopup();
        }

        private void MoveCameraTo(Vector3 position)
        {
            _editorCameraGO.Transform.Position = position - _editorCameraGO.Transform.Forward * 5.0f;
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

        public void SelectObject(GameObject go)
        {
            _selectedObjects.Add(go);
            go.Destroyed += OnSelectedDestroyed;
            var mrs = go.GetComponents<MeshRenderer>();
            foreach (var mr in mrs)
            {
                mr.OverrideTint = new TintInfo(new Vector3(1.0f), 0.6f);
            }
            Debug.Assert(!_colliderRenderers.ContainsKey(go));
            ColliderShapeRenderer renderer = GetColliderShapeRenderer();
            renderer.GameObject = go;
            _gs.AddFreeRenderItem(renderer);
            _colliderRenderers.Add(go, renderer);
            _newSelectedObject = go;
        }

        /// <summary>Returns a new or cached ColliderShapeRenderer.</summary>
        private ColliderShapeRenderer GetColliderShapeRenderer()
        {
            if (_cachedColliderRenderers.Count == 0)
            {
                return new ColliderShapeRenderer(_as, _gs.Context, RgbaFloat.Cyan);
            }
            else
            {
                ColliderShapeRenderer ret = _cachedColliderRenderers[_cachedColliderRenderers.Count - 1];
                _cachedColliderRenderers.RemoveAt(_cachedColliderRenderers.Count - 1);
                return ret;
            }
        }

        private void Deselect(GameObject go)
        {
            UntintAndUnsubscribe(go);
            _selectedObjects.Remove(go);
        }

        public void ClearSelection()
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
                mr.OverrideTint = new TintInfo();
            }
            ColliderShapeRenderer renderer = _colliderRenderers[go];
            _gs.RemoveFreeRenderItem(renderer);
            _colliderRenderers.Remove(go);
            _cachedColliderRenderers.Add(renderer);
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
                if (_focusNameField)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusNameField = false;
                }
                if (ImGui.InputText("###GoNameInput", _goNameBuffer.Buffer, _goNameBuffer.Length, InputTextFlags.Default, null))
                {
                    go.Name = _goNameBuffer.ToString();
                }
                ImGui.SameLine();
                ImGui.Text(go.ID.ToString());

            }

            ImGui.EndChildFrame();
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

            ImGui.PushStyleColor(ColorTarget.Button, RgbaFloat.Blue.ToVector4());
            if (_componentCopySource != null)
            {
                if (ImGui.Button($"Paste {_componentCopySourceType}"))
                {
                    go.AddComponent(_componentCopySource.GetAsset(_as.ProjectDatabase.DefaultSerializer));
                }
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
                    if (ImGui.MenuItem("Copy Component", component.GetType() != s_transformType))
                    {
                        _componentCopySource = new InMemoryAsset<Component>();
                        _componentCopySource.UpdateAsset(_as.ProjectDatabase.DefaultSerializer, componentAsObject);
                        _componentCopySourceType = component.GetType().Name;
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
