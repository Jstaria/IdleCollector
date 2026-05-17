using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Numerics;

namespace SkillTreeCreationTool
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Point renderSize = new Point(480, 270);
        public static string MainSceneName = "MainScene";

        private SkillTree skillTree;
        private Camera camera;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            SceneManager.Initialize(MainSceneName, _graphics, renderSize);

            int screenWidth = Renderer.RenderSize.X;
            int screenHeight = Renderer.RenderSize.Y;

            Point screenHalf = new Point(screenWidth / 2, screenHeight / 2);

            camera = new Camera(screenWidth, screenHeight, 20, 1.25f);
            Renderer.CurrentCamera = camera;
            Updater.AddToUpdate(camera);

            FileIO.InDebug = true;

            skillTree = new SkillTree();
            SceneManager.AddToScene(skillTree);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Drawing.Initialize(_spriteBatch);

            ResourceAtlas.LoadTextures(Content, "Content/Textures/", "Textures");

            skillTree.AddToken(new Point(0, 0));
            skillTree.SetTokenParent(new Point(0, 0), new Point(2,2));
            skillTree.AddToken(new Point(2, -2));
            skillTree.SetTokenParent(new Point(0, 0), new Point(2, -2));

            skillTree.AddToken(new Point(-2, 2));
            skillTree.SetTokenParent(new Point(0, 0), new Point(-2, 2));
            skillTree.AddToken(new Point(-2, -2));
            skillTree.SetTokenParent(new Point(0, 0), new Point(-2, -2));

            skillTree.AddToken(new Point(2, 4));
            skillTree.SetTokenParent(new Point(2, 2), new Point(2, 4));
            skillTree.AddToken(new Point(4, 2));
            skillTree.SetTokenParent(new Point(2, 2), new Point(4, 2));

            //skillTree.SaveSkillTree();

            //Renderer.AddToSceneDraw((sb) => { sb.Draw(ResourceAtlas.GetTexture("board1"), new Vector2(0,0), Color.White);  });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            Updater.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Gray);

            Renderer.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
