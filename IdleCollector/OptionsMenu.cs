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
    
    internal static class MenuData
    {
        public static int divisions = 20;
    }

    internal class OptionsMenu : IScene
    {
        private OptionsState currentState;
        private float optionsFade = 0;
        private Texture2D prevRender;
        private Vector2 StartingPostion = new Vector2(Renderer.UIBounds.Size.ToVector2().X / 2, Renderer.UIBounds.Size.ToVector2().Y / 2);
        private Dictionary<string, Dictionary<string, UIContainer>> buttons;
        private Dictionary<string, UIContainer> currentMenu;
        private Dictionary<string, UIContainer> prevMenu;
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
                    ["Audio"] = new MenuButton(GetButtonConfig("Audio", -1, () => CallMenu("Audio"))),
                    ["Display"] = new MenuButton(GetButtonConfig("Display", 0, () => CallMenu("Display"))),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", 1, RequestExit)),
                },
                ["Audio"] = new()
                {
                    ["Master Volume"] = new Slider(GetButtonConfig("Master Volume", -1.25f), (value) => { SetVolume(value, "MasterVolume"); }),
                    ["Test"] = new MenuButton(GetButtonConfig("Test", -.5f, () => { Debug.WriteLine("Audio Test"); })),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", .5f, () => CallMenu("Main"))),
                },
                ["Display"] = new()
                {
                    ["Test"] = new MenuButton(GetButtonConfig("Test", -.5f, () => { Debug.WriteLine("Display Test"); })),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", .5f, () => CallMenu("Main"))),
                },
            };

            currentMenu = buttons["Main"];
        }

        private async void SceneEnter()
        {
            currentMenu = buttons["Main"];

            foreach (UIContainer container in currentMenu.Values)
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

            foreach (UIContainer container in currentMenu.Values)
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

            foreach (UIContainer container in currentMenu.Values)
                container.Draw(sb);

            if (prevMenu != null)
                foreach (UIContainer container in prevMenu.Values)
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

            foreach (UIContainer container in currentMenu.Values)
            {
                if (timer < .1f) container.PrevUpdate(gameTime);
                else container.Update(gameTime);
            }
            if (prevMenu != null)
                foreach (UIContainer container in prevMenu?.Values)
                    container.PrevUpdate(gameTime);
        }

        private void CallMenu(string name)
        {
            timer = 0;
            prevMenu = currentMenu;
            currentMenu = buttons[name];

            foreach (UIContainer container in prevMenu?.Values)
                container.DropOut();

            foreach (UIContainer container in currentMenu.Values)
                container.DropIn();
        }

        private void SetVolume(int value, string vName)
        {
            VolumeController vCon = VolumeController.Instance;
            
            vCon.IncrementVolume(vName, 1.0f / MenuData.divisions * value);
        }

        private ButtonConfig GetButtonConfig(string buttonText, float i, OnButtonClick func = null)
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

    public abstract class UIContainer 
    {
        public Vector2 drawPosition;
        public Spring2D positionSpring;

        protected List<IRenderable> renderables = new();

        protected Vector2 buttonPosition;
        protected Vector2 outOfScreen = new Vector2(Renderer.ScreenSize.X / 2, -200);

        public void DropIn() => positionSpring.RestPosition = buttonPosition;
        public void DropOut() => positionSpring.RestPosition = outOfScreen;

        public virtual void Draw(SpriteBatch sb) 
        {
            foreach (IRenderable ren in renderables)
                ren.Draw(sb);
        }
        public abstract void Update(GameTime gameTime);
        public abstract void PrevUpdate(GameTime gameTime);
    }

    public class MenuButton: UIContainer
    {
        public Button button;

        public MenuButton(ButtonConfig config)
        {
            button = new Button(Game1.Instance, config);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;

            renderables.Add(button);
        }

        public override void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }
        public override void PrevUpdate(GameTime gameTime)
        {
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }
    }

    public class Slider: UIContainer
    {
        private int min = 0, max = 0, value = 0;
        private int sensitivity = 20;
        private Button button;
        private CustomText text;
        public delegate void OnSlide(int value);
        public delegate int GetValue();
        private OnSlide slide;
        private ButtonConfig config;

        public Slider(ButtonConfig config, OnSlide slide)
        {
            config.OnClick = GetMouseInput;
            config.bounds.Height = 20 * Renderer.UIScaler.X;

            ButtonConfig config2 = config;
            config2.texts = null;
            config2.rotationRadians = 0.001f;
            config2.textures = new[] { ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(5, 8)) };
            config2.font = null;
            
            this.slide = slide;

            text = new CustomText(
                Game1.Instance,
                "Fonts/" + config.font, 
                config.texts[0], 
                config.bounds.Location.ToVector2(), 
                config.bounds.Size.ToVector2(), 
                shadowColor: config.shadowColor, color: config.fontColor);
            text.Refresh();

            button = new Button(Game1.Instance, config2);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;

            renderables.Add(button);
        }

        public override void PrevUpdate(GameTime gameTime)
        {
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }

        public override void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }

        public override void Draw(SpriteBatch sb)
        {
            base.Draw(sb);

            Vector2 pos = drawPosition - config.bounds.Size.ToVector2() / 2;

        }

        private async void GetMouseInput()
        {
            int mouseX = Input.GetMouseScreenPos().X;

            while (Input.IsLeftButtonDown())
            {
                int newX = Input.GetMouseScreenPos().X;
                int delta = newX - mouseX;

                if (MathF.Abs(delta) > sensitivity)
                {
                    slide.Invoke(MathF.Sign(delta));
                }

                await Task.Delay(1);
            }
        }
    }
}
