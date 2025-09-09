using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using IdleEngine;
using Particles;
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

        private bool MainScene = true;
        private Point position = new Point(20, 50);
        private Spring springX = new Spring(20, .75f, 250);
        private Spring springY = new Spring(20, .75f, 150);
        private Texture2D circle;
        private TestCollider[] testColliders = new TestCollider[4];

        private bool[] bools = new bool[4];

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.HardwareModeSwitch = false;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            SceneManager.Initialize("Main Scene", _graphics, new Point(240 * 2, 135 * 2));

            testColliders[0] = new TestCollider();
            testColliders[0].CollisionType = CollisionType.Circle;
            testColliders[0].Radius = 25;
            testColliders[0].Position = new Point(100, 75);
            testColliders[0].Bounds = new Rectangle(-25, -25, 50, 50);
            testColliders[1] = new TestCollider();
            testColliders[1].CollisionType = CollisionType.Circle;
            testColliders[1].Radius = 50;
            testColliders[1].Bounds = new Rectangle(-50, -50, 100, 100);
            testColliders[1].Position = new Point(300, 75);
            testColliders[2] = new TestCollider();
            testColliders[2].CollisionType = CollisionType.Rectangle;
            testColliders[2].Bounds = new Rectangle(0, 0, 100, 50);
            testColliders[2].Position = new Point(300, 75);
            testColliders[3] = new TestCollider();
            testColliders[3].CollisionType = CollisionType.Rectangle;
            testColliders[3].Bounds = new Rectangle(0, 0, 75, 75);
            testColliders[3].Position = new Point(300, 75);

            Renderer.AddToSceneDraw((_spriteBatch) =>
            {
                int size = 24;
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(20, 50, size, size), ResourceAtlas.GetTileRect("green"), Color.White, .5f, Vector2.Zero, SpriteEffects.None, 0);
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(position, new Point(size, size)), ResourceAtlas.GetTileRect("red"), Color.White);
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(Input.GetMousePos(), new Point(size, size)), ResourceAtlas.GetTileRect("blue"), Color.White);
            });

            SceneManager.AddScene("Sample Scene");
            Renderer.AddToSceneDraw("Sample Scene", (_spriteBatch) =>
            {
                int size = 24;
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(testColliders[2].Position, testColliders[2].Bounds.Size), bools[2] ? ResourceAtlas.GetTileRect("green") : ResourceAtlas.GetTileRect("red"), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0);
                _spriteBatch.Draw(circle, new Rectangle(testColliders[1].Position + testColliders[1].Bounds.Location, testColliders[1].Bounds.Size), bools[1] ? Color.Purple : Color.Green);
                _spriteBatch.Draw(circle, new Rectangle(testColliders[0].Position + testColliders[0].Bounds.Location, testColliders[0].Bounds.Size), bools[0] ? Color.Blue : Color.Aqua);
            });

            Updater.AddToUpdate(UpdateType.Standard, (gameTime) =>
            {
                if (Input.IsButtonDownOnce(Keys.F11) || Input.AreButtonsDownOnce(Keys.LeftAlt, Keys.Enter))
                    Renderer.ToggleFullScreen();

                if (Input.IsButtonDownOnce(Keys.Space))
                {
                    SceneManager.SwapScene(MainScene ? "Sample Scene" : "Main Scene");
                    MainScene = !MainScene;
                }


            });

            Updater.AddToSceneUpdate(UpdateType.Standard, (gameTime) => { position.Y += Input.GetMouseScrollDelta() * 5; });
            Updater.AddToSceneUpdate("Sample Scene", UpdateType.Controlled, (gameTime) =>
            {
                springX.Update();
                springY.Update();

                
                bool temp1 = CollisionHelper.CheckForCollision(testColliders[0], testColliders[2]);

                bools[0] = temp1;
                bools[2] = temp1;

                Point pos = Input.GetMousePos();
                testColliders[0].Position = pos;
            });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            circle = Content.Load<Texture2D>("Textures/circle");
            ResourceAtlas.LoadTilemap(Content, "Textures/atlas", "../../../Content/Textures/atlasKeys.txt", new Point(3, 1));

            //LoadEffects();
        }

        #region LoadEffects 

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
