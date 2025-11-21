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

        public void ControlledUpdate(GameTime gameTime)
        {
            
        }

        public void UIDraw(SpriteBatch sb)
        {
            sb.Draw(prevRender, Renderer.UIBounds, Color.White);
            sb.Draw(ResourceAtlas.GetTexture("tempPause"), Renderer.UIBounds, Color.White * optionsFade);
        }

        public void Draw(SpriteBatch sb)
        {

        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }

        public void StandardUpdate(GameTime gameTime)
        {
            
        }
    }
}
