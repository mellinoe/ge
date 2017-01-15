using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Engine
{
    public class Game
    {
        private bool _running;
        public bool LimitFrameRate { get; set; } = true;
        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private readonly List<GameObject> _destroyList = new List<GameObject>();
        private Stopwatch _sw;
        private long _previousFrameTicks;
        private readonly TimeControlSystem _timeSystem;
        private ConcurrentQueue<Action> _endOfFrameActions = new ConcurrentQueue<Action>();

        public SystemRegistry SystemRegistry { get; } = new SystemRegistry();

        public double DesiredFramerate { get; set; } = 60.0;

        public Game()
        {
            _timeSystem = new TimeControlSystem();
            SystemRegistry.Register(_timeSystem);
            SystemRegistry.Register(new GameObjectQuerySystem(_gameObjects));
            GameObject.InternalConstructed += OnGameObjectConstructed;
            GameObject.InternalDestroyRequested += OnGameObjectDestroyRequested;
            GameObject.InternalDestroyCommitted += OnGameObjectDestroyCommitted;
        }

        internal void FlushDeletedObjects()
        {
            foreach (GameObject go in _destroyList)
            {
                go.CommitDestroy();
            }
            _destroyList.Clear();
        }

        private void OnGameObjectConstructed(GameObject go)
        {
            go.SetRegistry(SystemRegistry);
            lock (_gameObjects)
            {
                _gameObjects.Add(go);
            }
        }

        private void OnGameObjectDestroyRequested(GameObject go)
        {
            _destroyList.Add(go);
        }

        private void OnGameObjectDestroyCommitted(GameObject go)
        {
            lock (_gameObjects)
            {
                _gameObjects.Remove(go);
            }
        }

        public void RunMainLoop()
        {
#if RELEASE
            try
            {
#endif
                RunMainLoopCore();
#if RELEASE
            }
            catch (Exception e)
            {
                LogException(e);
            }
#endif
        }

        private void LogException(Exception e)
        {
            CrashLogHelper.LogUnhandledException(e, this);
        }

        private void RunMainLoopCore()
        {
            _running = true;

            _sw = Stopwatch.StartNew();
            while (_running)
            {
                double desiredFrameTime = 1000.0 / DesiredFramerate;
                long currentFrameTicks = _sw.ElapsedTicks;
                double deltaMilliseconds = (currentFrameTicks - _previousFrameTicks) * (1000.0 / Stopwatch.Frequency);

                while (LimitFrameRate && deltaMilliseconds < desiredFrameTime)
                {
                    Thread.Sleep(0);
                    currentFrameTicks = _sw.ElapsedTicks;
                    deltaMilliseconds = (currentFrameTicks - _previousFrameTicks) * (1000.0 / Stopwatch.Frequency);
                }

                _previousFrameTicks = currentFrameTicks;

                FlushDeletedObjects();

                var systemEnumerator = SystemRegistry.GetSystemsEnumerator();
                while (systemEnumerator.MoveNext())
                {
                    var kvp = systemEnumerator.Current;
                    GameSystem system = kvp.Value;
                    float deltaSeconds = (float)deltaMilliseconds / 1000.0f;
                    system.Update(deltaSeconds * _timeSystem.TimeScale);
                }

                RunEndOfFrameActions();
            }

            var cleanupEnumerator = SystemRegistry.GetSystemsEnumerator();
            while (cleanupEnumerator.MoveNext())
            {
                var kvp = cleanupEnumerator.Current;
                GameSystem system = kvp.Value;
                if (system is IDisposable)
                {
                    ((IDisposable)system).Dispose();
                }
            }
        }

        private void RunEndOfFrameActions()
        {
            Action a;
            while (_endOfFrameActions.TryDequeue(out a))
            {
                a();
            }
        }

        public void NewSceneLoaded()
        {
            var systemEnumerator = SystemRegistry.GetSystemsEnumerator();
            while (systemEnumerator.MoveNext())
            {
                var kvp = systemEnumerator.Current;
                kvp.Value.OnNewSceneLoaded();
            }

            ResetDeltaTime();
        }

        public void QueueEndOfFrameAction(Action a)
        {
            _endOfFrameActions.Enqueue(a);
        }

        public void Exit()
        {
            _running = false;
        }

        public void ResetDeltaTime()
        {
            _sw?.Restart();
            _previousFrameTicks = 0L;
        }
    }

    public class TimeControlSystem : GameSystem
    {
        public float DefaultTimescale { get; set; }

        public float TimeScale { get; set; }

        public TimeControlSystem()
        {
            DefaultTimescale = 1f;
            TimeScale = 1f;
        }

        protected override void UpdateCore(float deltaSeconds)
        {
        }

        protected override void OnNewSceneLoadedCore()
        {
            TimeScale = DefaultTimescale;
        }
    }
}
