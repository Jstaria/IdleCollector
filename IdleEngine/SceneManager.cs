using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public static class SceneManager
    {
        private static List<string> sceneNames;
        private static string currentSceneName;

        internal static string CurrentSceneName => currentSceneName;

        public static void Initialize(string sceneName, GraphicsDeviceManager deviceManager, Point renderSize)
        {
            sceneNames = new List<string>();
            currentSceneName = sceneName;

            Updater.Initialize();
            Renderer.Initialize(deviceManager, renderSize);
            Input.Initialize();

            AddScene(sceneName);
        }

        public static void SwapScene(string sceneName)
        {
            if (!sceneNames.Contains(sceneName))
                AddScene(sceneName);

            currentSceneName = sceneName;

            Updater.SwapScene(sceneName);
            Renderer.SwapScene(sceneName);
        }

        public static void AddScene(string sceneName)
        {
            sceneNames.Add(sceneName);

            Updater.AddScene(sceneName);
            Renderer.AddScene(sceneName);
        }
    }
}
