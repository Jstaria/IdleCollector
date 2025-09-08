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
        public static Dictionary<string, Dictionary<UpdateType,OnUpdate>> UpdateEvents;
        private static Dictionary<UpdateType,OnUpdate> UpdateEvent;
        private static Dictionary<UpdateType, OnUpdate> IndependentUpdateEvent;

        private readonly static int slowFrameSkip = 3;
        private static int frameCount = 0;

        public static int ControlledUpdateCount { get; set; }

        public static void Initialize()
        {
            UpdateEvents = new();
            
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
            for (int i = 0; i < ControlledUpdateCount; i++)
            {
                UpdateEvent[UpdateType.Controlled]?.Invoke(gameTime);
                IndependentUpdateEvent[UpdateType.Controlled]?.Invoke(gameTime);
            }

            UpdateEvent[UpdateType.Standard]?.Invoke(gameTime);
            IndependentUpdateEvent[UpdateType.Standard]?.Invoke(gameTime);

            if ((frameCount = ++frameCount % 60) % slowFrameSkip == 0)
            {
                UpdateEvent[UpdateType.Slow]?.Invoke(gameTime);
                IndependentUpdateEvent[UpdateType.Slow]?.Invoke(gameTime);
            }
        }

        internal static void SwapScene(string sceneName)
        {
            UpdateEvent = UpdateEvents[sceneName];
        }

        internal static void AddScene(string sceneName)
        {
            if (UpdateEvents.ContainsKey(sceneName))
                throw new Exception(String.Format("Events already has scene: {0}", sceneName));

            UpdateEvents.Add(sceneName, new());

            UpdateEvents[sceneName].Add(UpdateType.Controlled, (GameTime gameTime) => { });
            UpdateEvents[sceneName].Add(UpdateType.Standard, (GameTime gameTime) => { });
            UpdateEvents[sceneName].Add(UpdateType.Slow, (GameTime gameTime) => { });
        }

        /// <summary>
        /// Adds to a scene's update loop, requires swap
        /// </summary>
        public static void AddToSceneUpdate(string sceneName, IUpdatable updatable) => AddToSceneUpdate(sceneName, updatable.Type, updatable.Update);
        /// <summary>
        /// Adds to a scene's update loop, requires swap
        /// </summary>
        public static void AddToSceneUpdate(string sceneName, UpdateType type, OnUpdate func) => UpdateEvents[sceneName][type] += func;
        /// <summary>
        /// Adds to current scene's update loop, doesn't requires swap
        /// </summary>
        public static void AddToSceneUpdate(IUpdatable updatable) => AddToSceneUpdate(updatable.Type, updatable.Update);
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
        public static void AddToUpdate(IUpdatable updatable) => AddToUpdate(updatable.Type, updatable.Update);
        /// <summary>
        /// Adds to scene independent update loop, doesn't requires swap
        /// </summary>
        public static void AddToUpdate(UpdateType type, OnUpdate func) => IndependentUpdateEvent[type] += func;
    }
}
