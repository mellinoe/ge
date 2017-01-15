using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Veldrid.Platform;
using System.Runtime.InteropServices;

namespace Engine
{
    public class InputSystem : GameSystem
    {
        private readonly Window _window;

        private readonly HashSet<Veldrid.Platform.Key> _currentlyPressedKeys = new HashSet<Veldrid.Platform.Key>();
        private readonly HashSet<Veldrid.Platform.Key> _newKeysThisFrame = new HashSet<Veldrid.Platform.Key>();

        private readonly HashSet<Veldrid.Platform.MouseButton> _currentlyPressedMouseButtons = new HashSet<Veldrid.Platform.MouseButton>();
        private readonly HashSet<Veldrid.Platform.MouseButton> _newMouseButtonsThisFrame = new HashSet<Veldrid.Platform.MouseButton>();

        private readonly List<Action<InputSystem>> _callbacks = new List<Action<InputSystem>>();

        private Vector2 _previousSnapshotMousePosition;

        public Vector2 MousePosition
        {
            get
            {
                return CurrentSnapshot.MousePosition;
            }
            set
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Point screenPosition = _window.ClientToScreen(new Point((int)value.X, (int)value.Y));
                    Mouse.SetPosition(screenPosition.X, screenPosition.Y);
                    var cursorState = Mouse.GetCursorState();
                    Point windowPoint = _window.ScreenToClient(new Point(cursorState.X, cursorState.Y));
                    _previousSnapshotMousePosition = new Vector2(windowPoint.X / _window.ScaleFactor.X, windowPoint.Y / _window.ScaleFactor.Y);
                }
                else
                {
                    Point screenPosition = new Point((int)value.X, (int)value.Y);
                    Mouse.SetPosition(screenPosition.X, screenPosition.Y);
                    var cursorState = Mouse.GetCursorState();
                    _previousSnapshotMousePosition = new Vector2(cursorState.X / _window.ScaleFactor.X, cursorState.Y / _window.ScaleFactor.Y);
                }
            }
        }

        public Vector2 MouseDelta { get; private set; }

        public InputSnapshot CurrentSnapshot { get; private set; }

        public InputSystem(Window window)
        {
            _window = window;
            window.FocusGained += WindowFocusGained;
            window.FocusLost += WindowFocusLost;
        }

        /// <summary>
        /// Registers an anonmyous callback which is invoked every time the InputSystem is updated.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        public void RegisterCallback(Action<InputSystem> callback)
        {
            _callbacks.Add(callback);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            UpdateFrameInput(_window.GetInputSnapshot());
            foreach (var callback in _callbacks)
            {
                callback(this);
            }
        }

        public void WindowFocusLost()
        {
            ClearState();
        }

        public void WindowFocusGained()
        {
            ClearState();
        }

        protected override void OnNewSceneLoadedCore()
        {
            ClearState();
        }

        private void ClearState()
        {
            _currentlyPressedKeys.Clear();
            _newKeysThisFrame.Clear();
            _currentlyPressedMouseButtons.Clear();
            _newMouseButtonsThisFrame.Clear();
        }

        public bool GetKey(Veldrid.Platform.Key Key)
        {
            return _currentlyPressedKeys.Contains(Key);
        }

        public bool GetKeyDown(Veldrid.Platform.Key Key)
        {
            return _newKeysThisFrame.Contains(Key);
        }

        public bool GetMouseButton(Veldrid.Platform.MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public bool GetMouseButtonDown(Veldrid.Platform.MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public void UpdateFrameInput(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            MouseDelta = CurrentSnapshot.MousePosition - _previousSnapshotMousePosition;
            _previousSnapshotMousePosition = CurrentSnapshot.MousePosition;

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent ke = keyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            IReadOnlyList<MouseEvent> mouseEvents = snapshot.MouseEvents;
            for (int i = 0; i < mouseEvents.Count; i++)
            {
                MouseEvent me = mouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private void MouseUp(Veldrid.Platform.MouseButton MouseButton)
        {
            _currentlyPressedMouseButtons.Remove(MouseButton);
            _newMouseButtonsThisFrame.Remove(MouseButton);
        }

        private void MouseDown(Veldrid.Platform.MouseButton MouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(MouseButton))
            {
                _newMouseButtonsThisFrame.Add(MouseButton);
            }
        }

        private void KeyUp(Veldrid.Platform.Key Key)
        {
            _currentlyPressedKeys.Remove(Key);
            _newKeysThisFrame.Remove(Key);
        }

        private void KeyDown(Veldrid.Platform.Key Key)
        {
            if (_currentlyPressedKeys.Add(Key))
            {
                _newKeysThisFrame.Add(Key);
            }
        }
    }
}
