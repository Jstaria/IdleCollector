using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using IdleEngine;

namespace IdleCollector
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            ResourceAtlas.LoadTilemap(Content, "Textures/atlas", "../../../Content/Textures/atlasKeys.txt", new Point(3, 1));

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            _spriteBatch.Begin(samplerState:SamplerState.PointClamp);

            _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(100, 200, 64, 64), ResourceAtlas.GetTileRect("red"), Color.White);
            _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(200, 200, 64, 64), ResourceAtlas.GetTileRect("green"), Color.White);
            _spriteBatch.Draw(ResourceAtlas.TilemapAtlas, new Rectangle(300, 200, 64, 64), ResourceAtlas.GetTileRect("blue"), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
