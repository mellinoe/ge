using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ge
{
    public class Game
    {
        private bool _running;
        private bool _limitFrameRate = true;
        private List<GameObject> _gameObjects = new List<GameObject>();

        public SystemRegistry SystemRegistry { get; } = new SystemRegistry();

        public IReadOnlyList<GameObject> GameObjects => _gameObjects;

        public double DesiredFramerate { get; set; } = 60.0;

        public Game()
        {
            GameObject.GameObjectConstructed += OnGameObjectConstructed;
        }

        private void OnGameObjectConstructed(GameObject go)
        {
            go.SetRegistry(SystemRegistry);
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

                while (_limitFrameRate && deltaMilliseconds < desiredFrameTime)
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
            }
        }

        public void Exit()
        {
            _running = false;
        }
    }
}