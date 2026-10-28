using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace IdleCollector
{
    public sealed class PauseMenu
    {
        private readonly Game game;
        private readonly string sceneName;
        private readonly Func<bool> canToggle;
        private readonly Action preparePause;
        private readonly Action mainAction;
        private readonly Action optionsAction;
        private readonly string mainLabel;
        private readonly CustomText title;
        private readonly Button resumeButton;
        private readonly Button mainButton;
        private readonly Button optionsButton;

        public bool IsPaused { get; private set; }
        public event Action<bool> OnIsPaused;

        public PauseMenu(
            Game game,
            string sceneName,
            Func<bool> canToggle = null,
            Action preparePause = null,
            Action mainAction = null,
            string mainLabel = "Main Menu",
            Action optionsAction = null)
        {
            this.game = game;
            this.sceneName = sceneName;
            this.canToggle = canToggle ?? (() => SceneManager.CurrentSceneName == sceneName);
            this.preparePause = preparePause;
            this.mainAction = mainAction;
            this.mainLabel = mainLabel;
            this.optionsAction = optionsAction;

            int centerX = Renderer.UIBounds.Center.X;
            int menuTop = Renderer.UIBounds.Bottom - 400;
            Color shadowColor = Color.Black * .45f;
            Color textColor = Color.White * 10f;

            title = new CustomText(
                game,
                "Fonts/DePixelHalbfettTitle",
                "<fx 0,2,0,0,0>Paused</fx>",
                new Vector2(centerX - 180, menuTop),
                new Vector2(360, 100),
                color: textColor,
                shadowColor: shadowColor);
            title.CenterText = true;
            title.Refresh();

            int buttonY = menuTop + 150;
            if (mainAction != null)
            {
                mainButton = CreateButton(mainLabel, centerX, buttonY, textColor, shadowColor);
                mainButton.OnClick += ReturnToMain;
                buttonY += 50;
            }

            if (optionsAction != null)
            {
                optionsButton = CreateButton("Options", centerX, buttonY, textColor, shadowColor);
                optionsButton.OnClick += () => optionsAction();
                buttonY += 50;
            }

            resumeButton = CreateButton("Resume", centerX, buttonY, textColor, shadowColor);
            resumeButton.OnClick += Resume;
        }

        public void Register()
        {
            Updater.AddToUpdate(UpdateType.Standard, Update);
            Updater.AddToLateUpdate(UpdateButtons);
            Renderer.AddToSceneUIDraw(sceneName, Draw);
        }

        public void Reset()
        {
            IsPaused = false;
            Updater.UnPauseScene();
        }

        private Button CreateButton(string label, int centerX, int y, Color textColor, Color shadowColor)
        {
            ButtonConfig config = new ButtonConfig
            {
                bounds = new Rectangle(centerX - 150, y, 300, 10 * Renderer.UIScaler.Y),
                texts = new[] { label, $"<fx 0,0,0,0,1>></fx> {label} <fx 0,0,0,0,2><</fx>" },
                font = "DePixelHalbfett",
                fontColor = textColor,
                shadowColor = shadowColor
            };

            return new Button(game, config);
        }

        private void Update(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.Escape) && canToggle())
                Toggle();
        }

        private void UpdateButtons(GameTime gameTime)
        {
            if (!IsPaused || SceneManager.CurrentSceneName != sceneName)
                return;

            resumeButton.StandardUpdate(gameTime);
            optionsButton?.StandardUpdate(gameTime);
            mainButton?.StandardUpdate(gameTime);
        }

        private void Toggle()
        {
            if (IsPaused)
            {
                Resume();
                return;
            }

            preparePause?.Invoke();
            IsPaused = true;
            Updater.PauseScene();
            OnIsPaused?.Invoke(true);
        }

        private void Resume()
        {
            IsPaused = false;
            Updater.UnPauseScene();
            OnIsPaused?.Invoke(false);
        }

        private void ReturnToMain()
        {
            Resume();
            mainAction();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (!IsPaused)
                return;

            int menuTop = Renderer.UIBounds.Bottom - 400;
            spriteBatch.Draw(
                ResourceAtlas.GetTexture("pauseBlock"),
                new Rectangle(0, menuTop, Renderer.UIBounds.Width, 300),
                Color.Black * .25f);

            title.Update(1f / 60f);
            title.Draw();
            mainButton?.Draw(spriteBatch);
            optionsButton?.Draw(spriteBatch);
            resumeButton.Draw(spriteBatch);
        }
    }
}
