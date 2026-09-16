using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Point renderSize = new Point(480, 270);
        public static string MainSceneName = "MainScene";

        private SkillTree skillTree;
        private SkillTreeEditor skillTreeEditor;
        private Camera camera;
        private static readonly Queue<char> textInput = new();

        public static Game Instance;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.TextInput += OnTextInput;
        }

        public static string ConsumeTextInput()
        {
            char[] characters = textInput.ToArray();
            textInput.Clear();
            return new string(characters);
        }

        private static void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (!char.IsControl(e.Character))
                textInput.Enqueue(e.Character);
        }

        protected override void Initialize()
        {
            Instance = this;

            SceneManager.Initialize(MainSceneName, _graphics, renderSize);
            SceneManager.AddScene("SkillTreeScene");
            SceneManager.SwapScene("SkillTreeScene");

            int screenWidth = Renderer.RenderSize.X;
            int screenHeight = Renderer.RenderSize.Y;

            Point screenHalf = new Point(screenWidth / 2, screenHeight / 2);

            camera = new Camera(screenWidth, screenHeight, 20, 1.25f);
            Renderer.CurrentCamera = camera;
            Updater.AddToUpdate(camera);

            FileIO.InDebug = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Drawing.Initialize(_spriteBatch);

            ResourceAtlas.LoadTextures(Content, "Content/Textures/", "Textures");
            ResourceAtlas.LoadTextures(Content, "Content/Textures/Icons/", "Icons");
            ResourceAtlas.LoadFonts(Content, "Content/Fonts/", "Fonts");
            ResourceAtlas.LoadEffects(Content, "Content/Effects", "Effects");

            skillTree = new SkillTree();
            skillTree.CollectToken(Point.Zero);
            SceneManager.AddToScene(skillTree);

            skillTreeEditor = new SkillTreeEditor(skillTree);
            SceneManager.AddToScene(skillTreeEditor);
            Renderer.AddToSceneUIDraw(skillTreeEditor.DrawUi);

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
