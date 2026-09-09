using IdleEngine;
using IdleEngine.PostProcesses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class OptionsMenu : IScene
    {
        private const string ConfigPath = "Content/Config/OptionsMenu";

        private OptionsState currentState;
        private float optionsFade;
        private Texture2D prevRender;
        private Vector2 startingPosition = new Vector2(Renderer.UIBounds.Size.ToVector2().X / 2, Renderer.UIBounds.Size.ToVector2().Y / 2);
        private Dictionary<string, Dictionary<string, UIContainer>> buttons;
        private Dictionary<string, UIContainer> currentMenu;
        private Dictionary<string, UIContainer> prevMenu;
        private float timer;
        private readonly Dictionary<string, SliderBinding> sliderBindings = new(StringComparer.Ordinal);
        private string optionsSceneName;

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public void Initialize(string optionsScene)
        {
            RegisterDefaultSliderBindings();

            optionsSceneName = optionsScene;
            SceneManager.AddScene(optionsScene);
            currentState = OptionsState.FadingIn;

            Updater.AddToSceneEnter(optionsSceneName, SceneEnter);
            Updater.AddToSceneUpdate(optionsSceneName, UpdateType.Standard, RequestExit);
            Renderer.AddToSceneUIDraw(optionsSceneName, UIDraw);

            CreateButtons();
        }

        public void RegisterSliderBinding(string name, Func<float> getValue, Action<float> setValue)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Slider binding names cannot be empty.", nameof(name));
            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));
            if (setValue == null)
                throw new ArgumentNullException(nameof(setValue));

            sliderBindings[name] = new SliderBinding(getValue, setValue);
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
                if (timer < .1f)
                    container.PrevUpdate(gameTime);
                else
                    container.Update(gameTime);
            }

            if (prevMenu != null)
                foreach (UIContainer container in prevMenu.Values)
                    container.PrevUpdate(gameTime);
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
                "slider" => CreateSlider(menuId, item),
                "checkbox" => new CheckBox(GetButtonConfig(item.Label, item.Row,
                    () => NudgeButtonScale(menuId, item.Id, nudgeValue),
                    () => HoverButton(menuId, item.Id)),
                    _ => ExecuteCheckAction(item),
                    () => GetCheckValue(item)),
                _ => throw new InvalidOperationException($"Unsupported options control type '{item.Type}' for '{item.Id}'.")
            };
        }

        private Slider CreateSlider(string menuId, MenuItemDefinition item)
        {
            SliderBinding binding = GetSliderBinding(item);
            int steps = item.Steps ?? MenuData.divisions;

            return new Slider(GetButtonConfig(item.Label, item.Row, null, () => HoverButton(menuId, item.Id)),
                value => SetSliderValue(binding, value, steps),
                () => GetSliderValue(binding, steps),
                steps);
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

            if (item.Action == "toggleBloom")
                return Bloom.Instance.Toggle();

            throw new InvalidOperationException($"Unsupported options checkbox action '{item.Action}'.");
        }

        private bool GetCheckValue(MenuItemDefinition item)
        {
            if (item.Binding == "IsMuted")
                return VolumeController.Instance.IsMuted;

            if (item.Binding == "hasBloom")
                return Bloom.Instance.UseBloom;

            throw new InvalidOperationException($"Unsupported options checkbox binding '{item.Binding}'.");
        }

        private static string RequireBinding(MenuItemDefinition item)
        {
            if (String.IsNullOrWhiteSpace(item.Binding))
                throw new InvalidOperationException($"Options slider '{item.Id}' requires a binding.");

            return item.Binding;
        }

        private void RegisterDefaultSliderBindings()
        {
            RegisterDefaultVolumeBinding(nameof(VolumeController.MasterVolume));
            RegisterDefaultVolumeBinding(nameof(VolumeController.MusicVolume));
            RegisterDefaultVolumeBinding(nameof(VolumeController.SoundEffectVolume));
            RegisterDefaultVolumeBinding(nameof(VolumeController.CharacterVolume));
            RegisterDefaultVolumeBinding(nameof(VolumeController.AmbientVolume));
            RegisterSliderBinding("bloomStrength", () => Bloom.Instance.Config.bloomStrength, Bloom.Instance.SetBloom);
        }

        private void RegisterDefaultVolumeBinding(string propertyName)
        {
            sliderBindings.TryAdd(propertyName, new SliderBinding(
                () => VolumeController.Instance.GetVolume(propertyName),
                value => VolumeController.Instance.ChangeVolume(propertyName, value)));
        }

        private SliderBinding GetSliderBinding(MenuItemDefinition item)
        {
            string name = RequireBinding(item);
            if (!sliderBindings.TryGetValue(name, out SliderBinding binding))
                throw new InvalidOperationException($"Options slider '{item.Id}' references an unregistered binding '{name}'.");

            return binding;
        }

        private static int SetSliderValue(SliderBinding binding, float value, int divisions)
        {
            binding.SetValue(MathHelper.Clamp(value, 0, 1));
            return GetSliderValue(binding, divisions);
        }

        private static int GetSliderValue(SliderBinding binding, int divisions)
        {
            return Math.Clamp((int)(binding.GetValue() * divisions), 0, divisions);
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
                if (currentState == OptionsState.FadingOut)
                    return;

                optionsFade = MathHelper.Lerp(optionsFade, 1, i / 100.0f);
                await Task.Delay(10);
            }
        }

        private void RequestExit(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.Escape) && SceneManager.CurrentSceneName != Game1.MainScene)
                RequestExit();
        }

        private async void RequestExit()
        {
            currentState = OptionsState.FadingOut;

            foreach (UIContainer container in currentMenu.Values)
                container.DropOut();

            for (int i = 0; i < 20; i++)
            {
                if (currentState == OptionsState.FadingIn)
                    return;

                optionsFade = MathHelper.Lerp(optionsFade, 0, i / 20.0f);
                await Task.Delay(3);
            }

            SceneManager.SwapPrevScene();
        }

        private void CallMenu(string name)
        {
            timer = 0;
            prevMenu = currentMenu;
            currentMenu = buttons[name];

            foreach (UIContainer container in prevMenu.Values)
                container.DropOut();

            foreach (UIContainer container in currentMenu.Values)
                container.DropIn();
        }

        private void HoverButton(string menuId, string itemId)
        {
            Button menuButton = buttons[menuId][itemId].button;
            menuButton.RotSpring.RestPosition = 0;

            Spring scaleSpring = menuButton.ScaleSpring;
            scaleSpring.RestPosition = 1.1f;
            menuButton.ClampSpringVelocity(scaleSpring);
        }

        private void NudgeButtonScale(string menuId, string itemId, float nudgeValue)
        {
            buttons[menuId][itemId].button.ScaleSpring.Nudge(nudgeValue);
        }

        private ButtonConfig GetButtonConfig(string buttonText, float row, OnButtonClick clickFunc = null, OnButtonHover hoverFunc = null)
        {
            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(startingPosition.ToPoint(), new Point(150, 30) * Renderer.UIScaler);
            config.bounds.Y += (int)(row * 175);
            config.texts = new string[] { buttonText, "<fx 0,0,0,0,1>></fx> " + buttonText + " <fx 0,0,0,0,2><</fx>" };
            config.font = "DePixelHalbfett";
            config.shadowColor = Color.Black * .4f;
            config.fontColor = Color.White;
            config.textures = [ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(1, 4))];
            config.rotationRadians = RandomHelper.Instance.GetFloat(-MathHelper.Pi, MathHelper.Pi) * .025f;
            config.OnClick += clickFunc;
            config.OnHover += hoverFunc;
            config.rotSpringAngFeq = 40;
            config.rotSpringDampRatio = 1;
            config.scaleSpringAngFeq = 40;
            config.scaleSpringDampRatio = .375f;

            return config;
        }

        public void UIDraw(SpriteBatch sb)
        {
            sb.Draw(prevRender, Renderer.UIBounds, Color.White);
            sb.DrawRect(Renderer.UIBounds, Color.Black * .4f * optionsFade);

            foreach (UIContainer container in currentMenu.Values)
                container.Draw(sb);

            if (prevMenu != null)
                foreach (UIContainer container in prevMenu.Values)
                    container.Draw(sb);
        }
    }
}
