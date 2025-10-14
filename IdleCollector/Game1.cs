using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using IdleEngine;
using System.Windows;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Threading;

namespace IdleCollector
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Button button;
        private GameManager _gameManager;
        public static string MainScene = "Main Scene";
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1440;
            _graphics.PreferredBackBufferHeight = 810;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.IsFullScreen = false;
            _graphics.HardwareModeSwitch = false;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            SceneManager.Initialize(MainScene, _graphics, new Point(240 * 2, 135 * 2));
            Drawing.Initialize(_spriteBatch);

            FileIO.InDebug = true;

            base.Initialize();
        }

        #region Load

        protected override async void LoadContent()
        {
            ResourceAtlas.LoadTilemap(Content, "Content/SaveData/atlasKeys.txt", "Textures/atlas");
            ResourceAtlas.LoadTextures(Content, "Content/Textures/", "Textures");
            ResourceAtlas.LoadFonts(Content, "Content/Fonts/", "Fonts");
            ResourceAtlas.LoadSongs(Content, "Content/Audio/", "Audio");
            ResourceAtlas.LoadSoundEffects(Content, "Content/SoundEffects/", "SoundEffects");
            Renderer.AddToSceneDraw((_spriteBatch) => { _spriteBatch.Draw(ResourceAtlas.GetTexture("screen"), new Rectangle(0, 0, 480, 270), Color.White); });

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.F11) || Input.AreButtonsDownOnce(Keys.LeftAlt, Keys.Enter))
                    Renderer.ToggleFullScreen();
            });

            _gameManager = new GameManager();

            LoadButtons();
            //LoadEffects();
        }

        protected void LoadButtons()
        {
            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(10 * Renderer.UIScaler.X, 50 * Renderer.UIScaler.Y, 192 * Renderer.UIScaler.X, 64 * Renderer.UIScaler.Y);
            config.textures = new[] { ResourceAtlas.GetTexture("newGame"), ResourceAtlas.GetTexture("newGameH") };

            button = new Button(config);
            button.OnClick += () => { SceneManager.SwapScene("Game Scene"); };

            Updater.AddToSceneUpdate(button);
            Renderer.AddToSceneUIDraw(button);

            SceneManager.SwapScene(MainScene);
        }

        protected void LoadEffects()
        {
            //Effect pixel = Content.Load<Effect>("Effects/Pixel");
            //EffectValues[] effectValues = new EffectValues[1];
            //effectValues[0].ints = new KeyValuePair<string, int>[] {
            //    new KeyValuePair<string, int>("pixelsX",1920),
            //    new KeyValuePair<string, int>("pixelsY", 1080),
            //    new KeyValuePair<string, int>("pixelation", 16)
            //};

            //BatchConfig pixelConfig = new BatchConfig(
            //    SpriteSortMode.Deferred,
            //    null,
            //    SamplerState.PointClamp,
            //    null, null,
            //    pixel,
            //    effectValues,
            //    Matrix.Identity
            //);

            //Renderer.AddEffectPass(pixelConfig);
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
            Renderer.Draw(_spriteBatch);

            _spriteBatch.Begin();

            _spriteBatch.End();

            base.Draw(gameTime);
        }
        #endregion
    }
}
