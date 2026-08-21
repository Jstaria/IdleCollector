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
using System.Threading;
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
            float nudgeValue = -10f;

            buttons = new()
            {
                ["Main"] = new()
                {
                    ["Audio"] = new MenuButton(GetButtonConfig("Audio", -1, () => { CallMenu("Audio"); NudgeButtonScale("Main", "Audio", nudgeValue); }, () => HoverButton("Main", "Audio"))),
                    ["Display"] = new MenuButton(GetButtonConfig("Display", 0, () => { CallMenu("Display"); NudgeButtonScale("Main", "Display", nudgeValue); }, () => HoverButton("Main", "Display"))),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", 1, () => { RequestExit(); NudgeButtonScale("Main", "Back", nudgeValue); }, () => HoverButton("Main", "Back"))),
                },
                ["Audio"] = new()
                {
                    ["Master Volume"] = new Slider(GetButtonConfig("Master", -2f), (value) => { return SetVolume(value, "MasterVolume"); }, () => GetVolumeValue("MasterVolume")),
                    ["Music Volume"] = new Slider(GetButtonConfig("Music", -1.5f), (value) => { return SetVolume(value, "MusicVolume"); }, () => GetVolumeValue("MusicVolume")),
                    ["Sound Effect Volume"] = new Slider(GetButtonConfig("Sound FX", -1f), (value) => { return SetVolume(value, "SoundEffectVolume"); }, () => GetVolumeValue("SoundEffectVolume")),
                    ["Character Volume"] = new Slider(GetButtonConfig("Character", -.5f), (value) => { return SetVolume(value, "CharacterVolume"); }, () => GetVolumeValue("CharacterVolume")),
                    ["Ambient Volume"] = new Slider(GetButtonConfig("Ambient", 0f), (value) => { return SetVolume(value, "AmbientVolume"); }, () => GetVolumeValue("AmbientVolume")),
                    ["Mute"] = new CheckBox(GetButtonConfig("Mute", .5f, () => { NudgeButtonScale("Audio", "Mute", nudgeValue); }, () => HoverButton("Audio", "Mute")), (value) => { return VolumeController.Instance.ToggleMute(); }, () => { return VolumeController.Instance.IsMuted; }),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", 1.5f, () => { CallMenu("Main"); NudgeButtonScale("Audio", "Back", nudgeValue); }, () => HoverButton("Audio", "Back"))),
                },
                ["Display"] = new()
                {
                    ["Test"] = new MenuButton(GetButtonConfig("Test", -.5f, () => { Debug.WriteLine("Display Test"); NudgeButtonScale("Display", "Test", nudgeValue); }, () => HoverButton("Display", "Test"))),
                    ["Back"] = new MenuButton(GetButtonConfig("Back", .5f, () => { CallMenu("Main"); NudgeButtonScale("Display", "Back", nudgeValue); }, () => HoverButton("Display", "Back"))),
                },
            };

            currentMenu = buttons["Main"];
        }

        #region // Menu Methods ======================================================================
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

        private void HoverButton(string buttonCategory, string button)
        {
            Button menuButton = buttons[buttonCategory][button].button;
            menuButton.RotSpring.RestPosition = 0; //menuButton.ButtonConfig.rotationRadians + MathHelper.ToRadians(5);
            menuButton.ScaleSpring.RestPosition = 1.10f;
        }

        private void NudgeButtonScale(string buttonCategory, string button, float nudgeValue)
        {
            Button menuButton = buttons[buttonCategory][button].button;
            menuButton.ScaleSpring.Nudge(nudgeValue);
        }

        private int SetVolume(float value, string vName)
        {
            VolumeController vCon = VolumeController.Instance;

            vCon.ChangeVolume(vName, value);

            return GetVolumeValue(vName);
        }
        private int GetVolumeValue(string vName)
        {
            VolumeController vCon = VolumeController.Instance;

            float volume = vCon.GetVolume(vName);

            return (int)(volume * MenuData.divisions);
        }

        private ButtonConfig GetButtonConfig(string buttonText, float i, OnButtonClick clickFunc = null, OnButtonHover hoverFunc = null)
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
            config.OnClick += clickFunc;
            config.OnHover += hoverFunc;
            config.rotSpringAngFeq = 40;
            config.rotSpringDampRatio = 1;
            config.scaleSpringAngFeq = 40;
            config.scaleSpringDampRatio = 1f;

            return config;
        }
    }
    #endregion

    #region // UI Bits ======================================================================
    public abstract class UIContainer
    {
        public Vector2 drawPosition;
        public Spring2D positionSpring;
        public Button button;

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

    public class MenuButton : UIContainer
    {

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

    public class Slider : UIContainer
    {
        private int min = 0, max = 0, value = 0;
        private int sliderWidth = 200, sliderStartX = 0, sliderEndX = 0;
        private int barWidth;
        private int sensitivity = 50;
        public delegate int OnSlide(float value);
        public delegate int GetValue();
        private OnSlide slide;
        private ButtonConfig config;
        private Vector2 textOffset;
        private Texture2D barTex;

        public Slider(ButtonConfig config, OnSlide slide, GetValue getValue)
        {
            barTex = ResourceAtlas.GetTexture("bar1");
            config.OnClick = GetMouseInput;
            config.bounds.Height = 20 * Renderer.UIScaler.X;
            config.OnDrawButton = DrawSlider;

            ButtonConfig config2 = config;
            textOffset = -new Vector2(config.bounds.Size.X / 4, 0);
            config2.textOffset = textOffset;
            config2.rotationRadians = 0.001f;
            config2.textures = new[] { ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(5, 8)) };
            //config2.font = null;

            this.slide = slide;

            this.config = config;
            button = new Button(Game1.Instance, config2);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;
            barWidth = this.sliderWidth / MenuData.divisions;
            drawPosition = positionSpring.Position;
            sliderStartX = (int)drawPosition.X;
            sliderEndX = sliderStartX + (barWidth + 4) * MenuData.divisions;

            // These are for checking mouse collision and needs to use scaled screen coords
            sliderStartX /= Renderer.UIScaler.X; sliderEndX /= Renderer.UIScaler.Y;

            renderables.Add(button);

            value = getValue.Invoke();
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

        public override void Draw(SpriteBatch sb) => base.Draw(sb);
        private void DrawSlider(SpriteBatch sb)
        {
            Vector2 pos = new Vector2(config.bounds.Width / 2, config.bounds.Height / 4);

            sensitivity = (int)(barWidth * .75f);

            for (int i = 0; i < MenuData.divisions; i++)
            {
                Color color = i >= value ? new Color(30, 15, 15) : Color.White;
                sb.Draw(barTex, new Rectangle((int)pos.X - 4, (int)pos.Y + 4, barWidth, barTex.Height * Renderer.UIScaler.Y), null, Color.Black * .25f, 0, Vector2.Zero, SpriteEffects.None, 0f);
                sb.Draw(barTex, new Rectangle((int)pos.X, (int)pos.Y, barWidth, barTex.Height * Renderer.UIScaler.Y), null, color, 0, Vector2.Zero, SpriteEffects.None, .01f);
                pos += Vector2.UnitX * (barWidth + 4);
            }
        }

        private async void GetMouseInput()
        {
            int mouseX = Input.GetMouseScreenPos().X + barWidth / 4;

            if (mouseX < sliderStartX || mouseX > sliderEndX) return;

            while (Input.IsLeftButtonDown())
            {
                int xDistance = MathHelper.Max(mouseX - sliderStartX, 0);
                int totalSliderWidth = sliderEndX - sliderStartX;
                float ratio = (float)xDistance / (float)totalSliderWidth;

                mouseX = Input.GetMouseScreenPos().X + barWidth / 4;
                value = slide.Invoke(ratio);

                await Task.Delay(1);
            }
        }
    }

    public class CheckBox : UIContainer
    {
        public delegate bool OnCheck(bool value);
        public delegate bool GetValue();

        private bool value = false;
        private ButtonConfig config;
        private Vector2 textOffset;
        private Texture2D boxTex;
        private Texture2D checkTex;
        private Texture2D shadowTex;
        public event OnCheck OnBoxCheck;

        public CheckBox(ButtonConfig config, OnCheck onCheck, GetValue getValue)
        {
            checkTex = ResourceAtlas.GetTexture("check16x16");
            boxTex = ResourceAtlas.GetTexture("checkboxThin");
            shadowTex = ResourceAtlas.GetTexture("dropShadow");

            config.bounds.Height = 20 * Renderer.UIScaler.X;
            config.OnClick += () => { value = onCheck.Invoke(!value); };
            config.OnDrawButton = DrawCheckbox;

            ButtonConfig config2 = config;
            textOffset = -new Vector2(config.bounds.Size.X / 4, 0);
            config2.textOffset = textOffset;
            config2.rotationRadians = 0.001f;
            config2.textures = new[] { ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(5, 8)) };

            this.config = config;
            button = new Button(Game1.Instance, config2);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;
            drawPosition = positionSpring.Position;
            value = getValue.Invoke();

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

        public override void Draw(SpriteBatch sb) => base.Draw(sb);
        private void DrawCheckbox(SpriteBatch sb)
        {
            int size = boxTex.Width * Renderer.UIScaler.X;

            Vector2 center = new Vector2(config.bounds.Width * .75f - boxTex.Width / 2, config.bounds.Height / 2);

            Rectangle drawRect = new Rectangle(
                (center - new Vector2(size * 0.5f)).ToPoint(),
                new Point(size));

            Rectangle shadowRect = new Rectangle(
                drawRect.Location + new Point(-6, 6),
                drawRect.Size);

            sb.Draw(boxTex, shadowRect, Color.Black * 0.25f);
            sb.Draw(boxTex, drawRect, Color.White);

            if (value)
            {
                sb.Draw(shadowTex, drawRect, Color.White);
                sb.Draw(checkTex, shadowRect, Color.Black * 0.25f);
                sb.Draw(checkTex, drawRect, Color.White);
            }
        }
    }
    #endregion
}
