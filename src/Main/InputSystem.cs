using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid.Platform;

namespace Ge
{
    public class InputSystem : GameSystem
    {
        private readonly Window _window;

        private readonly HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private readonly HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();

        private readonly List<Action<InputSystem>> _callbacks = new List<Action<InputSystem>>();

        public Vector2 MousePosition { get; private set; }

        public InputSnapshot CurrentSnapshot { get; private set; }

        public InputSystem(Window window)
        {
            _window = window;
        }

        /// <summary>
        /// Registers an anonmyous callback which is invoked every time the InputSystem is updated.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        public void RegisterCallback(Action<InputSystem> callback)
        {
            _callbacks.Add(callback);
        }

        public override void Update(float deltaSeconds)
        {
            UpdateFrameInput(_window.GetInputSnapshot());
            foreach (var callback in _callbacks)
            {
                callback(this);
            }
        }

        public bool GetKey(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public bool GetKeyDown(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        public bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public bool GetMouseButtonDown(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public void UpdateFrameInput(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            MousePosition = snapshot.MousePosition;
            foreach (var ke in snapshot.KeyEvents)
            {
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            foreach (var me in snapshot.MouseEvents)
            {
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

        private void MouseUp(MouseButton mouseButton)
        {
            _currentlyPressedMouseButtons.Remove(mouseButton);
            _newMouseButtonsThisFrame.Remove(mouseButton);
        }

        private void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
        }

        private void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
            }
        }
    }
}
