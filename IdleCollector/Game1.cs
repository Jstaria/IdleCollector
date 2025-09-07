using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using IdleEngine;
using Particles;
using System.Collections.Generic;
using System;

namespace IdleCollector
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D squareTex;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            Renderer.Initialize(GraphicsDevice);
            Renderer.DrawEvent += (_spriteBatch) =>
            {
                int size = 24;
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(20, 50, size, size), ResourceAtlas.GetTileRect("green"), Color.White, .5f, Vector2.Zero, SpriteEffects.None, 0);
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(20, 50, size, size), ResourceAtlas.GetTileRect("red"), Color.White);
                _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(Mouse.GetState().X * 240 / 1920, Mouse.GetState().Y * 135 / 1080, size, size), ResourceAtlas.GetTileRect("blue"), Color.White);
            };

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            squareTex = Content.Load<Texture2D>("Textures/grass");

            ResourceAtlas.LoadTilemap(Content, "Textures/atlas", "../../../Content/Textures/atlasKeys.txt", new Point(3, 1));

            LoadEffects();

            // TODO: use this.Content to load your game content here
        }

        #region LoadEffects 

        protected void LoadEffects()
        {
            Effect pixel = Content.Load<Effect>("Effects/Pixel");
            EffectValues[] effectValues = new EffectValues[1];
            effectValues[0].ints = new KeyValuePair<string, int>[] {
                new KeyValuePair<string, int>("pixelsX",1920),
                new KeyValuePair<string, int>("pixelsY", 1080),
                new KeyValuePair<string, int>("pixelation", 64)
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

            //Renderer.AddEffectPass( pixelConfig );
        }

        #endregion

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // TODO: Add your drawing code here

            Renderer.Draw(_spriteBatch, squareTex);

            //Matrix shear = Matrix.Identity;
            //shear.M21 = .25f * MathF.Sin(2 * (float)gameTime.TotalGameTime.TotalSeconds) * MathF.Cos((float)gameTime.TotalGameTime.TotalSeconds);
            //Matrix rotation = Matrix.CreateRotationZ(MathF.PI);
            //Matrix translation = Matrix.CreateTranslation(new Vector3(400, 400, 0));
            //Matrix final = shear * rotation * translation;

            //for (int i = 0; i < 5; i++)
            //{
            //    shear = Matrix.Identity;
            //    shear.M21 = .25f * MathF.Sin(2 * (float)gameTime.TotalGameTime.TotalSeconds + i * .5f) * MathF.Cos((float)gameTime.TotalGameTime.TotalSeconds);
            //    rotation = Matrix.CreateRotationZ(MathF.PI);
            //    translation = Matrix.CreateTranslation(new Vector3(100 + 120 * i, 400, 0));
            //    final = shear * rotation * translation;

            //    VertexPositionTexture[] verts = new VertexPositionTexture[5];
            //}

            base.Draw(gameTime);
        }
    }
}
