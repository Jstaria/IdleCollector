using IdleEngine;
using IdleEngine.PostProcesses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Particles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using System.Windows;

namespace IdleCollector
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _fpsFont;
        private readonly Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private int _drawsSinceFpsUpdate;
        private string _fpsText = "FPS: 0";

        public static Game Instance;

        int count = 0;
        private Button button;
        private GameManager _gameManager;
        public static string MainScene = "Main Scene";
        public static float time;

        private bool _adjustingWindowSize;
        private Point _lastClientSize;

        public Game1()
        {
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;
            Window.ClientSizeChanged += (_, _) => Renderer.UpdateScreenSize(Window.ClientBounds.Size);

            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920 / 4;
            _graphics.PreferredBackBufferHeight = 1080 / 4;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.IsFullScreen = false;
            _graphics.HardwareModeSwitch = false;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            //IsFixedTimeStep = false;
            //_graphics.SynchronizeWithVerticalRetrace = false;
            //_graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(_spriteBatch);

            Instance = this;

            Renderer.UpdateScreenSize(GraphicsDevice.Viewport.Bounds.Size);
            _lastClientSize = new Point(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight);

            SceneManager.Initialize(MainScene, _graphics, new Point(240 * 2, 135 * 2));
            Drawing.Initialize(_spriteBatch);

            FileIO.InDebug = true;

            base.Initialize();
        }

        #region Helper
        private void OnClientSizeChanged(object? sender, EventArgs e)
        {
            Point size = Window.ClientBounds.Size;
            if (size.X <= 0 || size.Y <= 0)
                return;

            if (_adjustingWindowSize)
            {
                _lastClientSize = size;
                return;
            }

            if (_lastClientSize == Point.Zero)
            {
                _lastClientSize = size;
                return;
            }

            int widthChange = Math.Abs(size.X - _lastClientSize.X);
            int heightChange = Math.Abs(size.Y - _lastClientSize.Y);

            int width;
            int height;

            if (widthChange >= heightChange)
            {
                width = size.X;
                height = (int)Math.Round(width * 9f / 16f);
            }
            else
            {
                height = size.Y;
                width = (int)Math.Round(height * 16f / 9f);
            }

            _lastClientSize = new Point(width, height);

            if (size == _lastClientSize)
                return;

            _adjustingWindowSize = true;
            try
            {
                _graphics.PreferredBackBufferWidth = width;
                _graphics.PreferredBackBufferHeight = height;
                _graphics.ApplyChanges();
            }
            finally
            {
                _adjustingWindowSize = false;
            }
        }
        #endregion

        #region Load

        protected override async void LoadContent()
        {
            ResourceAtlas.LoadTilemap(Content, "Content/SaveData/atlasKeys.txt", "Textures/atlas");
            ResourceAtlas.LoadTextures(Content, "Content/Textures/", "Textures");
            ResourceAtlas.LoadFonts(Content, "Content/Fonts/", "Fonts");
            ResourceAtlas.LoadSongs(Content, "Content/Audio/", "Audio");
            ResourceAtlas.LoadSoundEffects(Content, "Content/SoundEffects/", "SoundEffects");
            ResourceAtlas.LoadEffects(Content, "Content/Effects/", "Effects");
            _fpsFont = ResourceAtlas.GetFont("DePixelKlein");

            Renderer.AddToSceneDraw((_spriteBatch) => { _spriteBatch.Draw(ResourceAtlas.GetTexture("screen"), new Rectangle(0, 0, 480, 270), Color.White); });

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.F11) || Input.AreButtonsDownOnce(Keys.LeftAlt, Keys.Enter))
                    Renderer.ToggleFullScreen();
            });

            _gameManager = new GameManager();

            LoadButtons();
            LoadEffects();
        }

        protected void LoadButtons()
        {
            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(10 * Renderer.UIScaler.X, 50 * Renderer.UIScaler.Y, 192 * Renderer.UIScaler.X, 64 * Renderer.UIScaler.Y);
            config.textures = new[] { ResourceAtlas.GetTexture("newGame"), ResourceAtlas.GetTexture("newGameH") };
            config.rotationRadians = 0;//MathHelper.PiOver4;

            button = new Button(Game1.Instance, config);
            button.OnClick += () =>
            {
                SceneManager.SwapScene("Game Scene");
                // Resets any current data in worldManager when entering the scene
                _gameManager.ResetWorld();
            };

            Updater.AddToSceneUpdate(button);
            Renderer.AddToSceneUIDraw(button);

            SceneManager.SwapScene(MainScene);
        }

        protected void LoadEffects()
        {
            Bloom BloomEffect = Bloom.Instance;

            Renderer.AddPostProcess("Background", BloomEffect);
        }

        #endregion

        #region Update & Draw
        protected override void Update(GameTime gameTime)
        {
            Updater.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _drawsSinceFpsUpdate++;
            double fpsElapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            if (fpsElapsedSeconds >= 0.5)
            {
                _fpsText = $"FPS: {(int)Math.Round(_drawsSinceFpsUpdate / fpsElapsedSeconds)}";
                _drawsSinceFpsUpdate = 0;
                _fpsStopwatch.Restart();
            }

            Renderer.Draw(_spriteBatch);

            _spriteBatch.Begin();

            if (_fpsFont != null)
                _spriteBatch.DrawString(_fpsFont, _fpsText, new Vector2(6, 6), Color.Black,
                    0f, Vector2.Zero, .25f, SpriteEffects.None, 0f);

            //if (online != null)
            //_spriteBatch.Draw(online, new Vector2(500, 100), Color.White);
            _spriteBatch.End();



            base.Draw(gameTime);
        }
        #endregion
    }
}
