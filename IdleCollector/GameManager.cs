using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static IdleCollector.GameManager;

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
        [JsonRequired] private string PauseScene = "Pause Scene";
        private Texture2D prevTexture;
        private Button menuButton;
        private bool isPaused;

        public delegate void IsPaused(bool isPaused);
        public event IsPaused OnIsPaused;
        #endregion

        #region // GameManager Instance
        private static GameManager instance;
        private GameTime gameTime;
        [JsonRequired] private string GameScene = "Game Scene";
        [JsonRequired] private string GardenScene = "Garden Scene";

        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public UpdateType Type { get; set; }
        [JsonIgnore] public Color Color { get; set; }

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
        private ResourceManager resourceManager;
        private Trail trail;

        protected delegate void SaveData();
        protected delegate void ResetData();

        protected event SaveData Save;
        protected event ResetData Reset;

        [JsonRequired] private float saveDelay = 30;
        private float saveTimer;

        private AudioController musicCon;

        private CustomText PauseText;

        private void SetupDebug()
        {

        }

        public GameManager()
        {
            Type = UpdateType.Standard;

            musicCon = AudioController.Instance;
            Updater.AddToUpdate(musicCon);

            Setup();
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

            // camera.Zoom -= Input.GetMouseScrollDelta() * .1f;

            SaveHeartbeat(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {

        }

        public void Draw(SpriteBatch sb)
        {
            
        }

        private void Setup()
        {
            SetupGameScene(GameScene);
            SetupWorld();
            SetupResources();
            SetupPlayer();
            SetupPause();

            SetupDebug();
        }

        private void SetupGameScene(string sceneName)
        {
            FileIO.WriteJsonTo(this, "Content/SaveData/GameData", Formatting.Indented);

            SceneManager.AddScene(sceneName);
            SceneManager.AddToIndependent(this);
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
            PauseText = new CustomText(Game1.Instance, "Fonts/DePixelHalbfett", "<fx 0,2,0,0>Paused</fx>", new Vector2((1920 - ResourceAtlas.GetFont("DePixelHalbfett").MeasureString("Paused...").X) / 2, 1080 - 200), new Vector2(1000, 100), padding: new Vector2(30, 30), shadowColor: Color.Black);
            PauseText.Refresh();

            Renderer.AddToSceneUIDraw(GameScene, (sb) => {
                PauseText.Update(1 / 60.0f);
                if (isPaused) PauseText.Draw();
            });

            SceneManager.AddScene(PauseScene);

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.Escape) && SceneManager.CurrentSceneName != Game1.MainScene)
                {
                    if (isPaused)
                    {
                        isPaused = false;
                        Updater.UnPauseScene();
                    }
                    else
                    {
                        isPaused = true;
                        Updater.PauseScene();
                    }

                    OnIsPaused?.Invoke(isPaused);
                }
            });
            Updater.AddToSceneEnter(PauseScene, () => { prevTexture = Renderer.GetLastRender(); });
            Renderer.AddToSceneUIDraw((sb) => { if (isPaused) sb.Draw(prevTexture, new Rectangle(Point.Zero, Renderer.RenderSize), Color.White); });
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

        private void SetupResources()
        {
            resourceManager = new();
            AddToSaveable(resourceManager);

            Updater.AddToSceneEnter(GameScene, () => resourceManager.MoveTo(new Vector2(20, Renderer.UIBounds.Height - 20)));
            Updater.AddToSceneUpdate(GameScene, resourceManager);
            Updater.AddToLateUpdate(resourceManager.LateUpdate);
            Renderer.AddToSceneUIDraw(GameScene, resourceManager);

            OnIsPaused += resourceManager.OnIsPaused;
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

                AmbienceController.Instance.PlayContAmbience();
            });
            Updater.AddToSceneExit(GameScene, () =>
            {
                Renderer.CurrentCamera.UseBounds = false;
                AmbienceController.Instance.KillAmbience();
            });
        }

        private void AddToSaveable(ISaveable saveable)
        {
            Save += saveable.Save;
            Reset += saveable.Reset;
        }

        private void SaveHeartbeat(GameTime gameTime)
        {
            saveTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (saveTimer > 0) return;

            saveTimer = saveDelay;
            Save?.Invoke();
        }

        public void ResetWorld()
        {
            worldManager.CreateWorld();
            int screenWidth = Renderer.RenderSize.X;
            int screenHeight = Renderer.RenderSize.Y;

            Point screenHalf = new Point(screenWidth / 2, screenHeight / 2);
            player.Position = screenHalf.ToVector2();
        }
    }
}
