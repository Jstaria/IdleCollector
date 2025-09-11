using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using IdleEngine;
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
        private Camera camera;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.HardwareModeSwitch = false;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            SceneManager.Initialize("Main Scene", _graphics, new Point(240 * 2, 135 * 2));
            
            camera = new Camera(480, 270, .05f);
            camera.SetTranslation(new Point(240, 135));
            Renderer.CurrentCamera = camera;

            SceneManager.AddScene("Sample Scene");

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.F11) || Input.AreButtonsDownOnce(Keys.LeftAlt, Keys.Enter))
                    Renderer.ToggleFullScreen();

                camera.Update(gameTime);
            });

            base.Initialize();
        }

        #region Load

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            ResourceAtlas.LoadTilemap(Content, "Textures/atlas", "../../../Content/Textures/atlasKeys.txt", new Point(3, 1));
            ResourceAtlas.LoadTextures(Content, "../../../Content/Textures/", "Textures");

            Renderer.AddToSceneDraw((_spriteBatch) => { _spriteBatch.Draw(ResourceAtlas.GetTexture("screen"), new Rectangle(0, 0, 480, 270), Color.White); });

            LoadButtons();
            //LoadEffects();
        }

        protected void LoadButtons()
        {
            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(10, 50, 192, 64);
            config.textures = new[] { ResourceAtlas.GetTexture("newGame"), ResourceAtlas.GetTexture("newGameH") };

            button = new Button(config);
            button.OnClick += () => { SceneManager.SwapScene("Sample Scene"); };

            Updater.AddToSceneUpdate("Main Scene", button);
            Renderer.AddToSceneUIDraw("Main Scene", button);

            SceneManager.SwapScene("Main Scene");
        }

        protected void LoadEffects()
        {
            Effect pixel = Content.Load<Effect>("Effects/Pixel");
            EffectValues[] effectValues = new EffectValues[1];
            effectValues[0].ints = new KeyValuePair<string, int>[] {
                new KeyValuePair<string, int>("pixelsX",1920),
                new KeyValuePair<string, int>("pixelsY", 1080),
                new KeyValuePair<string, int>("pixelation", 16)
            };

            BatchConfig pixelConfig = new BatchConfig(
                SpriteSortMode.Deferred,
                null,
                SamplerState.PointClamp,
                null, null,
                pixel,
                effectValues,
                Matrix.Identity
            );

            Renderer.AddEffectPass(pixelConfig);
        }

        #endregion

        #region Update & Draw
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            Updater.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Renderer.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
        #endregion
    }
}
