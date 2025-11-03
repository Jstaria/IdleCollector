using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public enum UpdateType
    {
        Controlled,
        Standard,
        Slow
    }

    public static class Updater
    {
        public delegate void OnUpdate(GameTime gameTime);
        public delegate void OnSwap();
        private static Dictionary<string, Dictionary<UpdateType, OnUpdate>> UpdateEvents;
        private static Dictionary<string, OnSwap> OnEnterEvents;
        private static Dictionary<string, OnSwap> OnExitEvents;
        private static Dictionary<UpdateType, OnUpdate> UpdateEvent;
        private static Dictionary<UpdateType, OnUpdate> IndependentUpdateEvent;
        private static OnUpdate LateUpdate;

        private static List<string> PausedScenes;

        private readonly static int slowFrameSkip = 3;
        private static int frameCount = 0;

        public static int ControlledUpdateCount { get; set; }

        public static void Initialize()
        {
            ControlledUpdateCount = 1;
            OnEnterEvents = new();
            OnExitEvents = new();
            UpdateEvents = new();
            PausedScenes = new();

            UpdateEvent = new();
            UpdateEvent.Add(UpdateType.Controlled, (GameTime gameTime) => { });
            UpdateEvent.Add(UpdateType.Standard, (GameTime gameTime) => { });
            UpdateEvent.Add(UpdateType.Slow, (GameTime gameTime) => { });

            IndependentUpdateEvent = new();
            IndependentUpdateEvent.Add(UpdateType.Controlled, (GameTime gameTime) => { });
            IndependentUpdateEvent.Add(UpdateType.Standard, (GameTime gameTime) => { });
            IndependentUpdateEvent.Add(UpdateType.Slow, (GameTime gameTime) => { });
        }

        public static void Update(GameTime gameTime)
        {
            ControlledUpdate(gameTime);
            StandardUpdate(gameTime);
            SlowUpdate(gameTime);

            LateUpdate?.Invoke(gameTime);
        }

        private static void ControlledUpdate(GameTime gameTime)
        {
            for (int i = 0; i < ControlledUpdateCount; i++)
            {
                if (!PausedScenes.Contains(SceneManager.CurrentSceneName))
                    UpdateEvent[UpdateType.Controlled]?.Invoke(gameTime);
                IndependentUpdateEvent[UpdateType.Controlled]?.Invoke(gameTime);
            }
        }
        private static void StandardUpdate(GameTime gameTime)
        {
            if (!PausedScenes.Contains(SceneManager.CurrentSceneName))
                UpdateEvent[UpdateType.Standard]?.Invoke(gameTime);
            IndependentUpdateEvent[UpdateType.Standard]?.Invoke(gameTime);
        }

        private static void SlowUpdate(GameTime gameTime)
        {
            if ((frameCount = ++frameCount % 60) % slowFrameSkip == 0)
            {
                if (!PausedScenes.Contains(SceneManager.CurrentSceneName))
                    UpdateEvent[UpdateType.Slow]?.Invoke(gameTime);
                IndependentUpdateEvent[UpdateType.Slow]?.Invoke(gameTime);
            }
        }

        internal static void SwapScene(string sceneName)
        {
            OnExitEvents[SceneManager.CurrentSceneName].Invoke();
            OnEnterEvents[sceneName]?.Invoke();
            UpdateEvent = UpdateEvents[sceneName];
        }

        internal static void AddScene(string sceneName)
        {
            if (UpdateEvents.ContainsKey(sceneName))
                throw new Exception(String.Format("Events already has scene: {0}", sceneName));

            OnEnterEvents.Add(sceneName, () => { });
            OnExitEvents.Add(sceneName, () => { });
            UpdateEvents.Add(sceneName, new());
            UpdateEvents[sceneName].Add(UpdateType.Controlled, (GameTime gameTime) => { });
            UpdateEvents[sceneName].Add(UpdateType.Standard, (GameTime gameTime) => { });
            UpdateEvents[sceneName].Add(UpdateType.Slow, (GameTime gameTime) => { });
        }

        /// <summary>
        /// Adds to a scene's update loop, requires swap
        /// </summary>
        public static void AddToSceneUpdate(string sceneName, IUpdatable updatable)
        {
            AddToSceneUpdate(sceneName, UpdateType.Controlled, updatable.ControlledUpdate);
            AddToSceneUpdate(sceneName, UpdateType.Standard, updatable.StandardUpdate);
            AddToSceneUpdate(sceneName, UpdateType.Slow, updatable.SlowUpdate);
        }
        /// <summary>
        /// Adds to a scene's update loop, requires swap
        /// </summary>
        public static void AddToSceneUpdate(string sceneName, UpdateType type, OnUpdate func) => UpdateEvents[sceneName][type] += func;
        /// <summary>
        /// Adds to current scene's update loop, doesn't requires swap
        /// </summary>
        public static void AddToSceneUpdate(IUpdatable updatable)
        {
            AddToSceneUpdate(UpdateType.Controlled, updatable.ControlledUpdate);
            AddToSceneUpdate(UpdateType.Standard, updatable.StandardUpdate);
            AddToSceneUpdate(UpdateType.Slow, updatable.SlowUpdate);
        }
        /// <summary>
        /// Adds to current scene's update loop, doesn't requires swap
        /// </summary>
        public static void AddToSceneUpdate(UpdateType type, OnUpdate func)
        {
            UpdateEvent[type] += func;
            UpdateEvents[SceneManager.CurrentSceneName] = UpdateEvent;
        }
        /// <summary>
        /// Adds to scene independent update loop, doesn't requires swap
        /// </summary>
        public static void AddToUpdate(IUpdatable updatable)
        {
            AddToUpdate(UpdateType.Controlled, updatable.ControlledUpdate);
            AddToUpdate(UpdateType.Standard, updatable.StandardUpdate);
            AddToUpdate(UpdateType.Slow, updatable.SlowUpdate);
        }
        /// <summary>
        /// Adds to scene independent update loop, doesn't requires swap
        /// </summary>
        public static void AddToUpdate(UpdateType type, OnUpdate func) => IndependentUpdateEvent[type] += func;
        /// <summary>
        /// Add to event that is invoked on scene enter
        /// </summary>
        public static void AddToSceneEnter(string sceneName, OnSwap func) => OnEnterEvents[sceneName] += func;
        /// <summary>
        /// Add to event that is invoked on scene exit
        /// </summary>
        public static void AddToSceneExit(string sceneName, OnSwap func) => OnExitEvents[sceneName] += func;
        /// <summary>
        /// Add to event that is invoked after all other updates
        /// </summary>
        public static void AddToLateUpdate(OnUpdate func) => LateUpdate += func;

        public static void PauseScene()
        {
            if (!PausedScenes.Contains(SceneManager.CurrentSceneName))
                PausedScenes.Add(SceneManager.CurrentSceneName);
        }
        public static void UnPauseScene()
        {
            if (PausedScenes.Contains(SceneManager.CurrentSceneName))
                PausedScenes.Remove(SceneManager.CurrentSceneName);
        }
    }
}
