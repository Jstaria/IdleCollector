using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public enum OptionsState { FadingIn, FadingOut }

    internal class OptionsMenu : IScene
    {
        private OptionsState currentState;
        private float optionsFade = 0;
        private Texture2D prevRender;

        private Dictionary<string, Button> buttons;

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private string OptionsSceneName;

        public void Initialize(string OptionsScene)
        {
            OptionsSceneName = OptionsScene;
            SceneManager.AddScene(OptionsScene);

            currentState = OptionsState.FadingIn;

            Updater.AddToSceneEnter(OptionsSceneName, SceneEnter);
            Updater.AddToSceneUpdate(OptionsSceneName, UpdateType.Standard, RequestExit);
            Renderer.AddToSceneUIDraw(OptionsSceneName, UIDraw);

            CreateButtons();
        }

        private void CreateButtons()
        {
            Color shadowColor = Color.Black * .4f;
            Color fontColor = Color.White;
            Vector2 StartingPostion = new Vector2(600, 400);

            ButtonConfig TestConfig = new ButtonConfig();
            TestConfig.bounds = new Rectangle(StartingPostion.ToPoint(), new Point(150, 30) * Renderer.UIScaler);
            TestConfig.texts = new string[] { "Test Text", "<fx 0,0,0,0,1>></fx> Test Text <fx 0,0,0,0,2><</fx>" };
            TestConfig.font = "DePixelHalbfett";
            TestConfig.shadowColor = shadowColor;
            TestConfig.fontColor = fontColor;
            TestConfig.textures = [ResourceAtlas.GetTexture("board1")];
            TestConfig.rotationRadians = MathHelper.Pi / 6;

            ButtonConfig TestConfig2 = new ButtonConfig();
            TestConfig2.bounds = new Rectangle(StartingPostion.ToPoint(), new Point(150, 30) * Renderer.UIScaler);
            TestConfig2.texts = new string[] { "Test Text", "<fx 0,0,0,0,1>></fx> Test Text <fx 0,0,0,0,2><</fx>" };
            TestConfig2.font = "DePixelHalbfett";
            TestConfig2.shadowColor = shadowColor;
            TestConfig2.fontColor = fontColor;
            TestConfig2.textures = [ResourceAtlas.GetTexture("board1")];
            TestConfig2.rotationRadians = -MathHelper.Pi / 6;

            buttons = new()
            {
                ["test"] = new Button(Game1.Instance, TestConfig),
                ["test2"] = new Button(Game1.Instance, TestConfig2)
            }
            ;
        }

        private async void SceneEnter()
        {
            currentState = OptionsState.FadingIn;

            prevRender = Renderer.GetLastRender();
            for (int i = 0; i < 50; i++)
            {
                if (currentState == OptionsState.FadingOut) return;

                optionsFade = MathHelper.Lerp(optionsFade, 1, i / 50.0f);
                await Task.Delay(5);
            }
        }

        private async void RequestExit(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.Escape) && SceneManager.CurrentSceneName != Game1.MainScene)
            {
                currentState = OptionsState.FadingOut;

                for (int i = 0; i < 20; i++)
                {
                    if (currentState == OptionsState.FadingIn) return;

                    optionsFade = MathHelper.Lerp(optionsFade, 0, i / 20.0f);

                    await Task.Delay(3);
                }

                SceneManager.SwapPrevScene();
            }
        }

        public void UIDraw(SpriteBatch sb)
        {
            sb.Draw(prevRender, Renderer.UIBounds, Color.White);
            sb.Draw(ResourceAtlas.GetTexture("tempPause"), Renderer.UIBounds, Color.White * optionsFade);

            foreach (Button button in buttons.Values)
                button.Draw(sb);
        }

        public void Draw(SpriteBatch sb)
        {

        }

        public void ControlledUpdate(GameTime gameTime)
        {

        }

        public void SlowUpdate(GameTime gameTime)
        {

        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (Button button in buttons.Values)
                button.StandardUpdate(gameTime);
        }
    }
}
