using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class GameManager : IScene
    {
        // Player Variables
        private Player player;
        private Camera camera;
        private bool followPlayer;

        private string GameScene = "Game Scene";

        public UpdateType Type { get; set; }

        #region // GameManager Instance
        private static GameManager instance;
        private GameTime gameTime;

        public static GameManager Instance { 
            get { 
                if (instance == null) 
                    instance = new GameManager(); 
                return instance; 
            } 
        }
        #endregion

        public GameManager()
        {
            Type = UpdateType.Standard;

            SetupGameScene(GameScene);
            SetupPlayer();
        }

        public void Update(GameTime gameTime)
        {
            
        }

        public void Draw(SpriteBatch sb)
        {
            player.Draw(sb);
        }

        private void SetupGameScene(string sceneName)
        {
            SceneManager.AddScene(sceneName);
            SceneManager.AddToScene(sceneName, this);
        }

        private void SetupPlayer()
        {
            int screenWidth = Renderer.RenderSize.X;
            int screenHeight = Renderer.RenderSize.Y;

            Point screenHalf = new Point(screenWidth / 2, screenHeight / 2);

            player = new Player(
                ResourceAtlas.GetTexture("fox_outline"),
                screenHalf,
                new Rectangle(0, 0, 64, 64),
                new Point(4, 8));

            camera = new Camera(screenWidth, screenHeight, 20, 1);
            Renderer.CurrentCamera = camera;
            Updater.AddToUpdate(camera);
            followPlayer = false;

            Updater.AddToSceneEnter(GameScene, () => {
                followPlayer = true;
                camera.SetTranslation(player.Position.ToPoint()); 
            });
            Updater.AddToSceneExit(GameScene, () => {
                followPlayer = false;
                camera.SetTranslation(screenHalf);
            });
            Updater.AddToSceneUpdate(GameScene, player);
            Updater.AddToSceneUpdate(GameScene, UpdateType.Controlled, (gameTime) => {
                    if (followPlayer) camera.SetTarget(player.Position.ToPoint());
                });

            camera.SetTranslation(screenHalf);
        }

    }
}
