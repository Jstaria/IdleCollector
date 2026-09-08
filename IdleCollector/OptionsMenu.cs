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

    internal sealed class OptionsMenuConfig
    {
        public List<MenuDefinition> Menus { get; set; } = new();
    }

    internal sealed class MenuDefinition
    {
        public string Id { get; set; }
        public List<MenuItemDefinition> Items { get; set; } = new();
    }

    internal sealed class MenuItemDefinition
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public float Row { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string Binding { get; set; }
        public int? Steps { get; set; }
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

        private const string ConfigPath = "Content/Config/OptionsMenu";

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
            OptionsMenuConfig config = new();
            FileIO.ReadJsonInto(config, ConfigPath);

            buttons = new();
            foreach (MenuDefinition menu in config.Menus)
            {
                if (String.IsNullOrWhiteSpace(menu.Id) || buttons.ContainsKey(menu.Id))
                    throw new InvalidOperationException("Every options menu requires a unique id.");

                Dictionary<string, UIContainer> menuButtons = new();
                foreach (MenuItemDefinition item in menu.Items)
                {
                    if (String.IsNullOrWhiteSpace(item.Id) || menuButtons.ContainsKey(item.Id))
                        throw new InvalidOperationException($"Menu '{menu.Id}' contains an invalid or duplicate item id.");

                    menuButtons.Add(item.Id, CreateContainer(menu.Id, item));
                }

                buttons.Add(menu.Id, menuButtons);
            }

            if (!buttons.ContainsKey("Main"))
                throw new InvalidOperationException("Options menu config must define a Main menu.");

            currentMenu = buttons["Main"];
        }

        private UIContainer CreateContainer(string menuId, MenuItemDefinition item)
        {
            const float nudgeValue = -10f;

            return item.Type switch
            {
                "button" => new MenuButton(GetButtonConfig(item.Label, item.Row,
                    () => { ExecuteAction(item); NudgeButtonScale(menuId, item.Id, nudgeValue); },
                    () => HoverButton(menuId, item.Id))),
                "slider" => new Slider(GetButtonConfig(item.Label, item.Row, null, () => HoverButton(menuId, item.Id)),
                    value => SetVolume(value, RequireBinding(item), item.Steps ?? MenuData.divisions),
                    () => GetVolumeValue(RequireBinding(item), item.Steps ?? MenuData.divisions),
                    item.Steps ?? MenuData.divisions),
                "checkbox" => new CheckBox(GetButtonConfig(item.Label, item.Row,
                    () => NudgeButtonScale(menuId, item.Id, nudgeValue),
                    () => HoverButton(menuId, item.Id)),
                    _ => ExecuteCheckAction(item),
                    () => GetCheckValue(item)),
                _ => throw new InvalidOperationException($"Unsupported options control type '{item.Type}' for '{item.Id}'.")
            };
        }

        private void ExecuteAction(MenuItemDefinition item)
        {
            switch (item.Action)
            {
                case "openMenu":
                    if (String.IsNullOrWhiteSpace(item.Target) || !buttons.ContainsKey(item.Target))
                        throw new InvalidOperationException($"Button '{item.Id}' references an unknown menu.");

                    CallMenu(item.Target);
                    break;
                case "closeOptions":
                    RequestExit();
                    break;
                case "debugLog":
                    Debug.WriteLine(item.Target);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported options button action '{item.Action}'.");
            }
        }

        private bool ExecuteCheckAction(MenuItemDefinition item)
        {
            if (item.Action == "toggleMute")
                return VolumeController.Instance.ToggleMute();

            throw new InvalidOperationException($"Unsupported options checkbox action '{item.Action}'.");
        }

        private bool GetCheckValue(MenuItemDefinition item)
        {
            if (item.Binding == "IsMuted")
                return VolumeController.Instance.IsMuted;

            throw new InvalidOperationException($"Unsupported options checkbox binding '{item.Binding}'.");
        }

        private static string RequireBinding(MenuItemDefinition item)
        {
            if (String.IsNullOrWhiteSpace(item.Binding))
                throw new InvalidOperationException($"Options slider '{item.Id}' requires a binding.");

            return item.Binding;
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

            Spring scaleSpring = menuButton.ScaleSpring;
            scaleSpring.RestPosition = 1.1f;
            menuButton.ClampSpringVelocity(scaleSpring);
        }

        private void NudgeButtonScale(string buttonCategory, string button, float nudgeValue)
        {
            Button menuButton = buttons[buttonCategory][button].button;
            menuButton.ScaleSpring.Nudge(nudgeValue);
        }

        private int SetVolume(float value, string vName, int divisions)
        {
            VolumeController vCon = VolumeController.Instance;

            vCon.ChangeVolume(vName, value);

            return GetVolumeValue(vName, divisions);
        }
        private int GetVolumeValue(string vName, int divisions)
        {
            VolumeController vCon = VolumeController.Instance;

            float volume = vCon.GetVolume(vName);

            return (int)(volume * divisions);
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
            config.scaleSpringDampRatio = .375f;

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
        private int sliderWidth = 280, sliderStartX = 0, sliderEndX = 0;
        private int barWidth;
        private const float SegmentGap = 4f;
        private float segmentWidth;
        private float TrackWidth => sliderWidth;
        private int sensitivity = 50;
        private readonly int divisions;
        public delegate int OnSlide(float value);
        public delegate int GetValue();
        private OnSlide slide;
        private ButtonConfig config;
        private Vector2 textOffset;
        private Texture2D barTex;

        private int ScaledMouseX => (int)(Input.GetMouseScreenPos().X /*+ (barWidth / 4) * button.ScaleSpring.Position * 2*/);

        public Slider(ButtonConfig config, OnSlide slide, GetValue getValue, int divisions)
        {
            if (divisions <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisions), "Slider divisions must be greater than zero.");

            this.divisions = divisions;
            segmentWidth = (TrackWidth - SegmentGap * (divisions - 1)) / divisions;
            if (segmentWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisions), "Slider divisions leave no room for segments.");

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
            sliderEndX = sliderStartX + (int)(TrackWidth / Renderer.UIScaler.X);

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
            UpdateSliderBounds();
        }

        public override void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
            UpdateSliderBounds();
        }

        public override void Draw(SpriteBatch sb) => base.Draw(sb);
        private void DrawSlider(SpriteBatch sb)
        {
            Vector2 pos = new Vector2(config.bounds.Width / 2, config.bounds.Height / 4);

            sensitivity = (int)(segmentWidth * .75f);

            float startX = config.bounds.Width / 2f;
            for (int i = 0; i < divisions; i++)
            {
                Color color = i >= value ? new Color(30, 15, 15) : Color.White;
                int left = (int)MathF.Round(startX);
                int right = (int)MathF.Round(startX + segmentWidth);
                int width = Math.Max(right - left, 1);

                sb.Draw(barTex, new Rectangle(left - 4, (int)pos.Y + 4, width, barTex.Height * Renderer.UIScaler.Y), null, Color.Black * .25f, 0, Vector2.Zero, SpriteEffects.None, 0f);
                sb.Draw(barTex, new Rectangle(left, (int)pos.Y, width, barTex.Height * Renderer.UIScaler.Y), null, color, 0, Vector2.Zero, SpriteEffects.None, .01f);
                startX += segmentWidth + SegmentGap;
            }
        }

        private async void GetMouseInput()
        {
            int mouseX = ScaledMouseX;

            if (mouseX < sliderStartX || mouseX > sliderEndX) return;

            while (Input.IsLeftButtonDown())
            {
                float xDistance = MathHelper.Max(mouseX - sliderStartX, 0);
                float totalSliderWidth = sliderEndX - sliderStartX;
                float trackPosition = MathHelper.Clamp(xDistance / totalSliderWidth * TrackWidth, 0, TrackWidth);
                float ratio = GetDivisionCount(trackPosition) / (float)divisions;

                mouseX = ScaledMouseX;
                value = slide.Invoke(ratio);

                await Task.Delay(1);
            }
        }

        private int GetDivisionCount(float trackPosition)
        {
            float segmentCenter = segmentWidth / 2f;
            if (trackPosition < segmentCenter)
                return 0;

            float divisionStride = segmentWidth + SegmentGap;
            int count = 1 + (int)MathF.Floor((trackPosition - segmentCenter + divisionStride / 2f) / divisionStride);
            return Math.Clamp(count, 0, divisions);
        }

        private void UpdateSliderBounds()
        {
            sliderEndX = sliderStartX + (int)(TrackWidth / Renderer.UIScaler.X * button.ScaleSpring.Position);
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

            OnBoxCheck = onCheck;

            config.bounds.Height = 20 * Renderer.UIScaler.X;
            config.OnClick += () => { value = OnBoxCheck.Invoke(!value); };
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
