using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public static class Renderer
    {
        public delegate void OnDraw(SpriteBatch sb);
        private static Dictionary<string, OnDraw> DrawEvents;
        private static Dictionary<string, OnDraw> UIDrawEvents;
        private static event OnDraw DrawEvent;
        private static event OnDraw IndependentDrawEvent;
        private static event OnDraw UIDrawEvent;

        private static RenderTarget2D renderTexture;
        private static RenderTarget2D uiTexture;
        private static RenderTarget2D finalTexture;
        private static RenderTarget2D[] targets;
        private static Color[] colorData;

        private static BatchConfig renderTexConfig;
        private static List<BatchConfig> processes;

        private static GraphicsDeviceManager _graphics;

        public static Point TopLeftCorner { get => (-CurrentCamera.Position.ToVector2()).ToPoint(); }
        public static Point ScaledTopLeftCorner
        {
            get
            {
                Vector2 corner = TopLeftCorner.ToVector2();
                Vector2 origin = CameraBounds.Center.ToVector2();
                Vector2 direction = corner - origin;
                return (corner + direction / CurrentCamera.Zoom).ToPoint();
            }
        }
        public static Point RenderSize {  get; private set; }
        public static Point ScreenSize { get; private set; }
        public static Camera CurrentCamera { get; set; }
        public static Rectangle CameraBounds { get { return new Rectangle(TopLeftCorner, RenderSize); } }
        public static Rectangle ScaledCameraBounds { 
            get { 
                Vector2 center = CameraBounds.Center.ToVector2();
                Vector2 scaledCorner = ScaledTopLeftCorner.ToVector2();
                Vector2 halfSize = center - scaledCorner;
                Point scaledSize = (halfSize * 2).ToPoint();
                return new Rectangle(ScaledTopLeftCorner, scaledSize); 
            } 
        }

        public static void Initialize(GraphicsDeviceManager deviceManager, Point renderSize)
        {
            DrawEvents = new();
            UIDrawEvents = new();
            processes = new List<BatchConfig>();

            _graphics = deviceManager;

            RenderSize = renderSize;
            ScreenSize = _graphics.IsFullScreen ? 
                new Point(_graphics.GraphicsDevice.DisplayMode.Width, _graphics.GraphicsDevice.DisplayMode.Height) :
                new Point(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            renderTexture = new RenderTarget2D(_graphics.GraphicsDevice, renderSize.X, renderSize.Y);
            uiTexture = new RenderTarget2D(_graphics.GraphicsDevice, renderSize.X, renderSize.Y);
            renderTexConfig = new BatchConfig(
                SpriteSortMode.FrontToBack,
                null,
                SamplerState.PointClamp,
                null, null, null, null,
                Matrix.Identity
                );

            targets = new RenderTarget2D[] {
                new RenderTarget2D(_graphics.GraphicsDevice, renderTexture.Width, renderTexture.Height),
                new RenderTarget2D(_graphics.GraphicsDevice, renderTexture.Width, renderTexture.Height) };

            colorData = new Color[renderTexture.Width * renderTexture.Height];
        }

        public static void AddEffectPass(BatchConfig process) => processes.Add(process);

        public static void DrawToTexture(SpriteBatch sb)
        {
            sb.GraphicsDevice.SetRenderTarget(renderTexture);
            sb.GraphicsDevice.Clear(Color.CornflowerBlue);

            sb.Begin(
                renderTexConfig.sortMode,
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: renderTexConfig.effect,
                transformMatrix: CurrentCamera != null ? CurrentCamera.Transform : renderTexConfig.transformMatrix
                );
            DrawEvent?.Invoke(sb);
            IndependentDrawEvent?.Invoke(sb);
            sb.End();

            sb.GraphicsDevice.SetRenderTarget(null);

            for (int i = 0; i < processes.Count; i++)
            {
                ApplyEffectValues(processes[i], sb);
            }

            sb.GraphicsDevice.SetRenderTarget(uiTexture);
            sb.Begin(
                renderTexConfig.sortMode,
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: renderTexConfig.effect,
                transformMatrix: Matrix.Identity
                );

            Rectangle destinationRect = new Rectangle(0, 0, sb.GraphicsDevice.Viewport.Width, sb.GraphicsDevice.Viewport.Height);
            sb.Draw(finalTexture == null ? renderTexture : finalTexture, destinationRect, Color.White);
            UIDrawEvent?.Invoke(sb);

            sb.End();
            sb.GraphicsDevice.SetRenderTarget(null);
        }

        private static void ApplyEffectValues(BatchConfig process, SpriteBatch sb)
        {
            EffectValues[] values = process.effectValues;
            Effect effect = process.effect;

            RenderTarget2D currentSource = renderTexture;
            RenderTarget2D currentTarget = null;

            for (int i = 0; i < values.Length; i++)
            {
                currentTarget = targets[i % 2];
                sb.GraphicsDevice.SetRenderTarget(currentTarget);
                sb.GraphicsDevice.Clear(Color.Transparent);

                sb.Begin(
                    processes[i].sortMode,
                    blendState: processes[i].blendState,
                    samplerState: processes[i].samplerState,
                    depthStencilState: processes[i].depthStencilState,
                    rasterizerState: processes[i].rasterizerState,
                    effect: processes[i].effect,
                    transformMatrix: processes[i].transformMatrix
                );

                if (values[i].floats != null)
                    foreach (KeyValuePair<string, float> pair in values[i].floats)
                        effect.Parameters[pair.Key].SetValue(pair.Value);
                if (values[i].ints != null)
                    foreach (KeyValuePair<string, int> pair in values[i].ints)
                        effect.Parameters[pair.Key].SetValue(pair.Value);
                if (values[i].bools != null)
                    foreach (KeyValuePair<string, bool> pair in values[i].bools)
                        effect.Parameters[pair.Key].SetValue(pair.Value);
                if (values[i].matrices != null)
                    foreach (KeyValuePair<string, Matrix> pair in values[i].matrices)
                        effect.Parameters[pair.Key].SetValue(pair.Value);

                effect.CurrentTechnique.Passes[i].Apply();

                sb.Draw(currentSource, renderTexture.Bounds, Color.White);
                sb.End();

                RenderTarget2D temp = currentSource;
                currentSource = currentTarget;
                currentTarget = temp;
            }

            finalTexture = currentSource;

            //currentSource.GetData(colorData);
            //renderTexture.SetData(colorData);

            sb.GraphicsDevice.SetRenderTarget(null);
        }

        private static void SaveTextureToFile(Texture2D texture, string filename)
        {
            if (texture == null) return;

            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            using (FileStream stream = File.Create(filename))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
        }

        public static void Draw(SpriteBatch sb)
        {
            DrawToTexture(sb);

            Rectangle destinationRect = new Rectangle(0, 0, sb.GraphicsDevice.Viewport.Width, sb.GraphicsDevice.Viewport.Height);

            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(uiTexture, destinationRect, Color.White);
            sb.End();
        }

        internal static void SwapScene(string sceneName)
        {
            DrawEvent = DrawEvents[sceneName];
            UIDrawEvent = UIDrawEvents[sceneName];
        }

        internal static void AddScene(string sceneName)
        {
            if (DrawEvents.ContainsKey(sceneName))
                throw new Exception(String.Format("Events already has scene: {0}", sceneName));
            DrawEvents.Add(sceneName, (SpriteBatch sb) => { });
            UIDrawEvents.Add(sceneName, (SpriteBatch sb) => { });
        }

        public static void ToggleFullScreen()
        {
            _graphics.ToggleFullScreen();
            ScreenSize = _graphics.IsFullScreen ?
                    new Point(_graphics.GraphicsDevice.DisplayMode.Width, _graphics.GraphicsDevice.DisplayMode.Height) :
                    new Point(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        }

        /// <summary>
        /// Adds to a scene's draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneDraw(string sceneName, IRenderable drawable) => AddToSceneDraw(sceneName, drawable.Draw);
        /// <summary>
        /// Adds to a scene's draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneDraw(string sceneName, OnDraw func) => DrawEvents[sceneName] += func;
        /// <summary>
        /// Adds to current scene's draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneDraw(IRenderable drawable) => AddToSceneDraw(drawable.Draw);
        /// <summary>
        /// Adds to current scene's draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneDraw(OnDraw func)
        {
            DrawEvent += func;
            DrawEvents[SceneManager.CurrentSceneName] = DrawEvent;
        }
        /// <summary>
        /// Adds to scene independent draw loop, does not require scene swap
        /// </summary>
        public static void AddToDraw(IRenderable drawable) => AddToDraw(drawable.Draw);
        /// <summary>
        /// Adds to scene independent draw loop, does not require scene swap
        /// </summary>
        public static void AddToDraw(OnDraw func) => IndependentDrawEvent += func;
        /// <summary>
        /// Adds to a scene's draw ui loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneUIDraw(string sceneName, IRenderable drawable) => AddToSceneUIDraw(sceneName, drawable.Draw);
        /// <summary>
        /// Adds to a scene's draw ui loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneUIDraw(string sceneName, OnDraw func) => UIDrawEvents[sceneName] += func;
        /// <summary>
        /// Adds to current scene's ui draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneUIDraw(IRenderable drawable) => AddToSceneUIDraw(drawable.Draw);
        /// <summary>
        /// Adds to current scene's ui draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneUIDraw(OnDraw func)
        {
            UIDrawEvent += func;
            UIDrawEvents[SceneManager.CurrentSceneName] = UIDrawEvent;
        }
        public static Texture2D GetLastRender()
        {
            Texture2D tempTexture = new Texture2D(_graphics.GraphicsDevice, renderTexture.Width, renderTexture.Height);

            if (uiTexture != null)
            {
                uiTexture.GetData(colorData);
                tempTexture.SetData(colorData);
            }

            return tempTexture;
        }
    }
}
