using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private Vector2 StartingPostion = new Vector2(Renderer.UIBounds.Size.ToVector2().X / 2, Renderer.UIBounds.Size.ToVector2().Y / 2);
        private Dictionary<string, Dictionary<string, ButtonContainer>> buttons;
        private Dictionary<string, ButtonContainer> currentMenu;
        private Dictionary<string, ButtonContainer> prevMenu;
        private float timer;

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
            // I need a tree structure for the buttons here,
            // at least a simple one where each button container holds another list of button containers
            // To make the back button a lot easier
            buttons = new()
            {
                ["Main"] = new()
                {
                    ["Audio"] = new ButtonContainer(GetButtonConfig("Audio", () => { CallMenu("Audio"); }, -1)),
                    ["Display"] = new ButtonContainer(GetButtonConfig("Display", () => { CallMenu("Display"); }, 0)),
                    ["Back"] = new ButtonContainer(GetButtonConfig("Back", RequestExit, 1)),
                },
                ["Audio"] = new()
                {
                    ["Test"] = new ButtonContainer(GetButtonConfig("Test", () => { Debug.WriteLine("Audio Test"); }, -.5f)),
                    ["Back"] = new ButtonContainer(GetButtonConfig("Back", () => CallMenu("Main"), .5f)),
                },
                ["Display"] = new()
                {
                    ["Test"] = new ButtonContainer(GetButtonConfig("Test", () => { Debug.WriteLine("Display Test"); }, -.5f)),
                    ["Back"] = new ButtonContainer(GetButtonConfig("Back", () => CallMenu("Main"), .5f)),
                },
            };

            currentMenu = buttons["Main"];
        }

        private async void SceneEnter()
        {
            currentMenu = buttons["Main"];

            foreach (ButtonContainer container in currentMenu.Values)
                container.DropIn();

            currentState = OptionsState.FadingIn;

            prevRender = Renderer.GetLastRender();
            for (int i = 0; i < 100; i++)
            {
                if (currentState == OptionsState.FadingOut) return;

                optionsFade = MathHelper.Lerp(optionsFade, 1, i / 100.0f);
                await Task.Delay(10);
            }
        }

        private void RequestExit(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.Escape) && SceneManager.CurrentSceneName != Game1.MainScene)
            {
                RequestExit();
            }
        }
        private async void RequestExit()
        {
            currentState = OptionsState.FadingOut;

            foreach (ButtonContainer container in currentMenu.Values)
                container.DropOut();

            for (int i = 0; i < 20; i++)
            {
                if (currentState == OptionsState.FadingIn) return;

                optionsFade = MathHelper.Lerp(optionsFade, 0, i / 20.0f);

                await Task.Delay(3);
            }

            SceneManager.SwapPrevScene();
        }

        public void UIDraw(SpriteBatch sb)
        {
            sb.Draw(prevRender, Renderer.UIBounds, Color.White);
            sb.DrawRect(Renderer.UIBounds, Color.Black * .4f * optionsFade);
            //sb.Draw(ResourceAtlas.GetTexture("tempPause"), Renderer.UIBounds, Color.White * optionsFade);

            foreach (ButtonContainer container in currentMenu.Values)
                container.Draw(sb);

            if (prevMenu != null)
                foreach (ButtonContainer container in prevMenu.Values)
                    container.Draw(sb);
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
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (timer < .05f) return;

            foreach (ButtonContainer container in currentMenu.Values)
                container.Update(gameTime);
            if (prevMenu != null)
                foreach (ButtonContainer container in prevMenu?.Values)
                    container.Update(gameTime);
        }

        private void CallMenu(string name)
        {
            prevMenu = currentMenu;
            currentMenu = buttons[name];

            foreach (ButtonContainer container in prevMenu?.Values)
                container.DropOut();

            foreach (ButtonContainer container in currentMenu.Values)
                container.DropIn();
        }

        private ButtonConfig GetButtonConfig(string buttonText, OnButtonClick func, float i)
        {
            Color shadowColor = Color.Black * .4f;
            Color fontColor = Color.White;
            float rotationScale = .025f;

            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(StartingPostion.ToPoint(), new Point(150, 30) * Renderer.UIScaler);
            config.bounds.Y += (int)(i * 175);
            config.texts = new string[] { buttonText, "<fx 0,0,0,0,1>></fx> " + buttonText + " <fx 0,0,0,0,2><</fx>" };
            config.font = "DePixelHalbfett";
            config.shadowColor = shadowColor;
            config.fontColor = fontColor;
            config.textures = [ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(1, 4))];
            config.rotationRadians = RandomHelper.Instance.GetFloat(-MathHelper.Pi, MathHelper.Pi) * rotationScale;
            config.OnClick += func;

            return config;
        }
    }

    public class ButtonContainer
    {
        public Button button;
        public Vector2 drawPosition;
        public Spring2D positionSpring;

        private Vector2 buttonPosition;
        private Vector2 outOfScreen = new Vector2(Renderer.ScreenSize.X / 2, -200);

        public ButtonContainer(ButtonConfig config)
        {
            button = new Button(Game1.Instance, config);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .5f, outOfScreen);
            button.Position = outOfScreen;
        }

        public void DropIn() => positionSpring.RestPosition = buttonPosition;
        public void DropOut() => positionSpring.RestPosition = outOfScreen;

        public void Draw(SpriteBatch sb)
        {
            button.Draw(sb);
        }

        public void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }
    }

}
