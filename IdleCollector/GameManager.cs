using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class GameManager : IScene
    {
        #region // Player Variables
        private Player player;
        private Camera camera;
        private bool followPlayer;
        #endregion

        #region // Pause
        private string PauseScene = "Pause Scene";
        private Texture2D prevTexture;
        private Button menuButton;
        private bool isPaused;
        #endregion

        #region // GameManager Instance
        private static GameManager instance;
        private GameTime gameTime;
        private string GameScene = "Game Scene";

        public float LayerDepth { get; set; }
        public UpdateType Type { get; set; }
        public Color Color { get; set; }

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameManager();
                return instance;
            }
        }
        #endregion

        private WorldManager worldManager;

        private Trail trail;

        public GameManager()
        {
            Type = UpdateType.Standard;

            SetupGameScene(GameScene);
            SetupWorld();
            SetupPlayer();
            SetupPause();
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            
        }


        public void StandardUpdate(GameTime gameTime)
        {
            if (Input.AreButtonsDownOnce(Keys.OemPlus))
                Updater.ControlledUpdateCount++;
            if (Input.AreButtonsDownOnce(Keys.OemMinus))
                Updater.ControlledUpdateCount--;

            camera.Zoom -= Input.GetMouseScrollDelta() * .1f;
        }

        public void SlowUpdate(GameTime gameTime)
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

            SpriteFont font = ResourceAtlas.GetFont("DePixelHalbfett");
            string msg = "Hey Everyone!";
            Vector2 offset = font.MeasureString(msg) / 2;

            Renderer.AddToSceneUIDraw((sb) => { sb.DrawString(font, msg, new Vector2(100, 100) + offset, Color.Black, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f); });
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
                new Point(7, 8),
                8f);

            camera = new Camera(screenWidth, screenHeight, 20, 1.25f);
            Renderer.CurrentCamera = camera;
            Updater.AddToUpdate(camera);
            followPlayer = false;

            Updater.AddToSceneEnter(GameScene, () =>
            {
                followPlayer = true;
                camera.SetTranslation(player.Position.ToPoint());

                Rectangle adjustedWorldBounds = worldManager.WorldBounds;
                adjustedWorldBounds.X += 32;
                adjustedWorldBounds.Y += 32;
                adjustedWorldBounds.Width -= 64;
                adjustedWorldBounds.Height -= 64;

                player.WorldBounds = adjustedWorldBounds;
            });
            Updater.AddToSceneExit(GameScene, () =>
            {
                followPlayer = false;
                camera.SetTranslation(screenHalf);
            });
            SceneManager.AddToScene(GameScene, player);
            Updater.AddToSceneUpdate(GameScene, UpdateType.Controlled, (gameTime) =>
            {
                if (followPlayer) camera.SetTarget(player.Position.ToPoint());
                worldManager.ChangePlayerLayerDepth(player);
            });

            camera.SetTranslation(screenHalf);

            player.OnSpawn += worldManager.SpawnFlora;
            player.OnMove += worldManager.InteractWithFlora;
            worldManager.SyncWindParticlesToPlayer(player);
        }

        private void SetupPause()
        {
            TrailInfo info = new TrailInfo();
            info.TrackPosition = () => (Input.GetMousePos() * Renderer.UIScaler).ToVector2();
            info.TrackLayerDepth = () => 1;
            info.NumberOfSegments = 200;
            info.TrailLength = 400;
            info.SegmentsPerSecond = (float)info.NumberOfSegments * .5f;
            info.SegmentsRemovedPerSecond = info.SegmentsPerSecond * .75f;
            info.SegmentColor = (t) => { return Color.White; };
            info.TipWidth = 20;
            info.EndWidth = 0;

            trail = new Trail(info);

            SceneManager.AddScene(PauseScene);

            Updater.AddToSceneUpdate(PauseScene, trail);
            Renderer.AddToSceneUIDraw(PauseScene, trail);

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.Escape) && SceneManager.CurrentSceneName != Game1.MainScene)
                {
                    if (isPaused)
                    {
                        isPaused = false;
                        SceneManager.SwapPrevScene();
                    }
                    else
                    {
                        isPaused = true;
                        SceneManager.SwapScene(PauseScene);
                    }
                }
            });
            Updater.AddToSceneEnter(PauseScene, () => { prevTexture = Renderer.GetLastRender(); });
            Renderer.AddToSceneDraw(PauseScene, (sb) => { sb.Draw(prevTexture, new Rectangle(Point.Zero, Renderer.RenderSize), Color.White); });
            Renderer.AddToSceneUIDraw(PauseScene, (sb) =>
            {
                sb.Draw(ResourceAtlas.GetTexture("tempPause"), Renderer.UIBounds, Color.White);
            });

            ButtonConfig config = new ButtonConfig();
            config.textures = new[] { ResourceAtlas.GetTexture("tempMenu"), ResourceAtlas.GetTexture("tempMenuH") };
            config.bounds = new Rectangle(14 * Renderer.UIScaler.X, 14 * Renderer.UIScaler.Y, 127 * Renderer.UIScaler.X, 52 * Renderer.UIScaler.Y);

            menuButton = new Button(config);
            menuButton.OnClick += () => { SceneManager.SwapScene(Game1.MainScene); isPaused = false; };
            Renderer.AddToSceneUIDraw(PauseScene, menuButton);
            Updater.AddToSceneUpdate(PauseScene, menuButton);
            Updater.AddToSceneExit(PauseScene, () => { prevTexture?.Dispose(); });

        }

        private void SetupWorld()
        {
            worldManager = new WorldManager();
            SceneManager.AddToScene(GameScene, worldManager);
            Updater.AddToSceneEnter(GameScene, () =>
            {
                Rectangle adjustedWorldBounds = worldManager.WorldBounds;
                int offset = 4;
                adjustedWorldBounds.X += offset;
                adjustedWorldBounds.Y += offset;
                adjustedWorldBounds.Width -= offset * 2;
                adjustedWorldBounds.Height -= offset * 2;

                Renderer.CurrentCamera.SetBounds(adjustedWorldBounds);
                Renderer.CurrentCamera.UseBounds = true;
            });
            Updater.AddToSceneExit(GameScene, () =>
            {
                Renderer.CurrentCamera.UseBounds = false;
            });
        }
    }
}
