using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Engine
{
    public class Game
    {
        private bool _running;
        public bool LimitFrameRate { get; set; } = true;
        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private readonly List<GameObject> _destroyList = new List<GameObject>();

        public SystemRegistry SystemRegistry { get; } = new SystemRegistry();

        public double DesiredFramerate { get; set; } = 60.0;

        public Game()
        {
            SystemRegistry.Register(new GameObjectQuerySystem(_gameObjects));
            GameObject.InternalConstructed += OnGameObjectConstructed;
            GameObject.InternalDestroyRequested += OnGameObjectDestroyRequested;
            GameObject.InternalDestroyCommitted += OnGameObjectDestroyCommitted;
        }

        private void OnGameObjectConstructed(GameObject go)
        {
            go.SetRegistry(SystemRegistry);
            _gameObjects.Add(go);
        }

        private void OnGameObjectDestroyRequested(GameObject go)
        {
            _destroyList.Add(go);
        }

        private void OnGameObjectDestroyCommitted(GameObject go)
        {
            _gameObjects.Remove(go);
        }

        public void RunMainLoop()
        {
            _running = true;

            long previousFrameTicks = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (_running)
            {
                double desiredFrameTime = 1000.0 / DesiredFramerate;
                long currentFrameTicks = sw.ElapsedTicks;
                double deltaMilliseconds = (currentFrameTicks - previousFrameTicks) * (1000.0 / Stopwatch.Frequency);

                while (LimitFrameRate && deltaMilliseconds < desiredFrameTime)
                {
                    Thread.Sleep(0);
                    currentFrameTicks = sw.ElapsedTicks;
                    deltaMilliseconds = (currentFrameTicks - previousFrameTicks) * (1000.0 / Stopwatch.Frequency);
                }

                previousFrameTicks = currentFrameTicks;

                foreach (var kvp in SystemRegistry.GetSystems())
                {
                    GameSystem system = kvp.Value;
                    system.Update((float)deltaMilliseconds / 1000.0f);
                }

                foreach (GameObject go in _destroyList)
                {
                    go.CommitDestroy();
                }
                _destroyList.Clear();
            }
        }

        public void Exit()
        {
            _running = false;
        }
    }
}