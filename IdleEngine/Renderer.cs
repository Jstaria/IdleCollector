
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
        private static Dictionary<string, OnDraw> EarlyDrawEvents;
        private static Dictionary<string, OnDraw> DrawEvents;
        private static Dictionary<string, OnDraw> UIDrawEvents;
        private static Dictionary<string, OnDraw> DrawRenderTargets;
        private static event OnDraw DrawIndependentRTs;
        private static event OnDraw EarlyDrawEvent;
        private static event OnDraw DrawEvent;
        private static event OnDraw UIDrawEvent;
        private static event OnDraw IndependentDrawEvent;
        private static event OnDraw IndependentUIDrawEvent;
        private static RenderTarget2D renderTexture;
        private static RenderTarget2D uiOverlayTexture;
        private static RenderTarget2D uiTexture;
        private static RenderTarget2D postProcessedUiTexture;
        private static RenderTarget2D postProcessedNormalTexture;
        private static RenderTarget2D finalTexture;
        private static RenderTarget2D[] targets;
        private static Color[] colorData;
        private static Texture2D presentationFadeTexture;

        private static BatchConfig renderTexConfig;
        private static List<BatchConfig> processes;
        private static Dictionary<string, PostProcess> postProcesses;

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
        public static Point RenderSize { get; private set; }
        public static Point ScreenSize { get; private set; }
        public static Camera CurrentCamera { get; set; }
        public static Rectangle CameraBounds { get { return new Rectangle(TopLeftCorner, RenderSize); } }
        public static Rectangle ScaledCameraBounds
        {
            get
            {
                Vector2 center = CameraBounds.Center.ToVector2();
                Vector2 scaledCorner = ScaledTopLeftCorner.ToVector2();
                Vector2 halfSize = center - scaledCorner;
                Point scaledSize = (halfSize * 2).ToPoint();
                return new Rectangle(ScaledTopLeftCorner, scaledSize);
            }
        }
        public static Rectangle UIBounds => new Rectangle(0, 0, 1920, 1080);
        public static Point UIScaler => new Point(UIBounds.Width / RenderSize.X, UIBounds.Height / RenderSize.Y);
        public static Rectangle PresentationBounds => CalculatePresentationBounds(ScreenSize);

        public static void Initialize(GraphicsDeviceManager deviceManager, Point renderSize)
        {
            DrawRenderTargets = new();
            EarlyDrawEvents = new();
            DrawEvents = new();
            UIDrawEvents = new();
            processes = new List<BatchConfig>();
            postProcesses = new ();

            _graphics = deviceManager;

            RenderSize = renderSize;
            ScreenSize = _graphics.IsFullScreen ?
                new Point(_graphics.GraphicsDevice.DisplayMode.Width, _graphics.GraphicsDevice.DisplayMode.Height) :
                new Point(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            renderTexture = new RenderTarget2D(_graphics.GraphicsDevice, renderSize.X, renderSize.Y);

            uiOverlayTexture = new RenderTarget2D(_graphics.GraphicsDevice, UIBounds.Width, UIBounds.Height);
            uiTexture = new RenderTarget2D(_graphics.GraphicsDevice, UIBounds.Width, UIBounds.Height);
            renderTexConfig = new BatchConfig(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
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
        public static void AddPostProcess(string name, PostProcess postProcess)
        {
            if (postProcess == null)
                throw new ArgumentNullException(nameof(postProcess));

            if (postProcesses == null)
                postProcesses = new ();

            postProcesses.Add(name, postProcess);
        }
        public static PostProcess GetPostProcess(string name) => postProcesses[name];

        public static void ResetRenderTargetUI(SpriteBatch sb) => sb.GraphicsDevice.SetRenderTarget(uiTexture);
        public static void ResetRenderTarget(SpriteBatch sb) => sb.GraphicsDevice.SetRenderTarget(renderTexture);
        public static void ResetBeginDraw(SpriteBatch sb) => sb.Begin(
                renderTexConfig.sortMode,
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: renderTexConfig.effect,
                transformMatrix: renderTexConfig.transformMatrix
                );
        public static void ResetBeginDrawCam(SpriteBatch sb, BlendState state = null) => sb.Begin(
               renderTexConfig.sortMode,
               blendState: state == null ? renderTexConfig.blendState : state,
               samplerState: renderTexConfig.samplerState,
               depthStencilState: renderTexConfig.depthStencilState,
               rasterizerState: renderTexConfig.rasterizerState,
               effect: renderTexConfig.effect,
               transformMatrix: CurrentCamera.Transform
               );

        public static void ResetBeginDrawEffect(SpriteBatch sb, Effect effect) => sb.Begin(
                renderTexConfig.sortMode,
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: effect,
                transformMatrix: renderTexConfig.transformMatrix
                );

        public static void DrawToRenderTargets(SpriteBatch sb)
        {
            DrawIndependentRTs?.Invoke(sb);

            if (DrawRenderTargets.ContainsKey(SceneManager.CurrentSceneName))
                DrawRenderTargets[SceneManager.CurrentSceneName]?.Invoke(sb);
        }

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
            EarlyDrawEvent?.Invoke(sb);
            DrawEvent?.Invoke(sb);
            IndependentDrawEvent?.Invoke(sb);
            sb.End();

            sb.GraphicsDevice.SetRenderTarget(null);

            sb.GraphicsDevice.SetRenderTarget(uiOverlayTexture);
            sb.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: renderTexConfig.effect,
                transformMatrix: Matrix.Identity
                );

            UIDrawEvent?.Invoke(sb);
            IndependentUIDrawEvent?.Invoke(sb);
            sb.End();
            sb.GraphicsDevice.SetRenderTarget(null);

            RenderTarget2D normalTexture = finalTexture == null ? renderTexture : finalTexture;
            RenderTarget2D combinedTexture = null;
            var pp = postProcesses.Values.ToList();

            for (int i = 0; i < postProcesses.Count; i++)
            {
                if (pp[i].AppliesToScene(SceneManager.CurrentSceneName) &&
                    pp[i].Target == PostProcessTarget.Normal)
                    pp[i].Draw(sb, ref normalTexture, uiOverlayTexture, ref combinedTexture);
            }

            postProcessedNormalTexture = normalTexture;

            sb.GraphicsDevice.SetRenderTarget(uiTexture);
            sb.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(
                blendState: renderTexConfig.blendState,
                samplerState: renderTexConfig.samplerState,
                depthStencilState: renderTexConfig.depthStencilState,
                rasterizerState: renderTexConfig.rasterizerState,
                effect: renderTexConfig.effect,
                transformMatrix: Matrix.Identity
                );
            sb.Draw(normalTexture, uiTexture.Bounds, Color.White);
            sb.Draw(uiOverlayTexture, uiTexture.Bounds, Color.White);
            sb.End();
            sb.GraphicsDevice.SetRenderTarget(null);

            combinedTexture = uiTexture;
            for (int i = 0; i < postProcesses.Count; i++)
            {
                if (pp[i].AppliesToScene(SceneManager.CurrentSceneName) &&
                    pp[i].Target == PostProcessTarget.Combined)
                    pp[i].Draw(sb, ref normalTexture, uiOverlayTexture, ref combinedTexture);
            }

            postProcessedUiTexture = combinedTexture;
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

        public static Vector2 GetScreenPosition(Vector2 worldPosition)
        {
            Vector2 renderPos = Vector2.Transform(worldPosition, CurrentCamera.Transform);
            return renderPos * UIScaler.ToVector2();
        }
        public static Vector2 GetWorldPosition(Vector2 screenPosition)
        {
            Rectangle presentationBounds = PresentationBounds;
            float scaleX = RenderSize.X / (float)presentationBounds.Width;
            float scaleY = RenderSize.Y / (float)presentationBounds.Height;

            Vector2 worldPosition = new Vector2(
                (screenPosition.X - presentationBounds.X) * scaleX - CurrentCamera.Position.X,
                (screenPosition.Y - presentationBounds.Y) * scaleY - CurrentCamera.Position.Y
            );

            return worldPosition;
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
            DrawToRenderTargets(sb);
            DrawToTexture(sb);

            Rectangle destinationRect = CalculatePresentationBounds(sb.GraphicsDevice.Viewport.Bounds.Size);
            Texture2D presentationTexture = postProcessedUiTexture ?? uiTexture;
            Texture2D reflectionTexture = postProcessedNormalTexture ?? renderTexture;

            sb.GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);
            DrawMirroredPresentationEdges(sb, reflectionTexture, destinationRect, sb.GraphicsDevice.Viewport.Bounds);
            sb.Draw(presentationTexture, destinationRect, Color.White);
            sb.End();
        }

        private static void DrawMirroredPresentationEdges(
            SpriteBatch sb,
            Texture2D presentationTexture,
            Rectangle presentationBounds,
            Rectangle viewportBounds)
        {
            if (presentationBounds.X == 0 && presentationBounds.Y == 0)
                return;

            presentationFadeTexture ??= ResourceAtlas.GetTexture("pauseBlock");

            const int maxReflectionSize = 48;
            int halfWidth = presentationFadeTexture.Width / 2;
            Rectangle outerToInner = new Rectangle(halfWidth, 0, presentationFadeTexture.Width - halfWidth, presentationFadeTexture.Height);
            Rectangle innerToOuter = new Rectangle(0, 0, halfWidth, presentationFadeTexture.Height);

            float fadeAmt = .5f;

            if (presentationBounds.X > 0)
            {
                int barWidth = presentationBounds.X - viewportBounds.X;
                int reflectionWidth = Math.Min(maxReflectionSize, barWidth);
                Rectangle leftBar = new Rectangle(presentationBounds.X - reflectionWidth, presentationBounds.Y, reflectionWidth, presentationBounds.Height);
                Rectangle rightBar = new Rectangle(presentationBounds.Right, presentationBounds.Y, reflectionWidth, presentationBounds.Height);
                int sourceWidth = Math.Clamp(
                    (int)MathF.Ceiling(reflectionWidth * presentationTexture.Width / (float)presentationBounds.Width),
                    1,
                    presentationTexture.Width);

                sb.Draw(presentationTexture, leftBar, new Rectangle(0, 0, sourceWidth, presentationTexture.Height), Color.White * fadeAmt, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                sb.Draw(presentationTexture, rightBar, new Rectangle(presentationTexture.Width - sourceWidth, 0, sourceWidth, presentationTexture.Height), Color.White * fadeAmt, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                sb.Draw(presentationFadeTexture, leftBar, outerToInner, Color.White);
                sb.Draw(presentationFadeTexture, rightBar, innerToOuter, Color.White);
            }

            if (presentationBounds.Y > 0)
            {
                int barHeight = presentationBounds.Y - viewportBounds.Y;
                int reflectionHeight = Math.Min(maxReflectionSize, barHeight);
                Rectangle topBar = new Rectangle(presentationBounds.X, presentationBounds.Y - reflectionHeight, presentationBounds.Width, reflectionHeight);
                Rectangle bottomBar = new Rectangle(presentationBounds.X, presentationBounds.Bottom, presentationBounds.Width, reflectionHeight);
                int sourceHeight = Math.Clamp(
                    (int)MathF.Ceiling(reflectionHeight * presentationTexture.Height / (float)presentationBounds.Height),
                    1,
                    presentationTexture.Height);

                sb.Draw(presentationTexture, topBar, new Rectangle(0, 0, presentationTexture.Width, sourceHeight), Color.White * fadeAmt, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
                sb.Draw(presentationTexture, bottomBar, new Rectangle(0, presentationTexture.Height - sourceHeight, presentationTexture.Width, sourceHeight), Color.White * fadeAmt, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
                DrawVerticalPresentationFade(sb, outerToInner, topBar);
                DrawVerticalPresentationFade(sb, innerToOuter, bottomBar);
            }
        }

        private static void DrawVerticalPresentationFade(SpriteBatch sb, Rectangle source, Rectangle destination)
        {
            Vector2 scale = new Vector2(
                destination.Height / (float)source.Width,
                destination.Width / (float)source.Height);
            Vector2 position = new Vector2(destination.Right, destination.Top);

            sb.Draw(presentationFadeTexture, position, source, Color.White, MathHelper.PiOver2,
                Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static Rectangle CalculatePresentationBounds(Point availableSize)
        {
            float scale = Math.Min(
                availableSize.X / (float)UIBounds.Width,
                availableSize.Y / (float)UIBounds.Height);
            Point size = new Point(
                Math.Max(1, (int)MathF.Round(UIBounds.Width * scale)),
                Math.Max(1, (int)MathF.Round(UIBounds.Height * scale)));

            return new Rectangle(
                (availableSize.X - size.X) / 2,
                (availableSize.Y - size.Y) / 2,
                size.X,
                size.Y);
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
        /// Adds to a scene's early draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneEarlyDraw(string sceneName, IRenderable drawable) => AddToSceneEarlyDraw(sceneName, drawable.Draw);
        /// <summary>
        /// Adds to a scene's early draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneEarlyDraw(string sceneName, OnDraw func) => EarlyDrawEvents[sceneName] += func;
        /// <summary>
        /// Adds to current early scene's draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneEarlyDraw(IRenderable drawable) => AddToSceneEarlyDraw(drawable.Draw);
        /// <summary>
        /// Adds to current scene's draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneEarlyDraw(OnDraw func)
        {
            EarlyDrawEvent += func;
            EarlyDrawEvents[SceneManager.CurrentSceneName] = EarlyDrawEvent;
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
        public static void AddToDrawRT(OnDraw func) => DrawIndependentRTs += func;
        public static void AddToDrawRT(OnDraw func, string name)
        {
            if (!DrawRenderTargets.ContainsKey(name))
                DrawRenderTargets.Add(name, null);

            DrawRenderTargets[name] += func;
        }
        public static Texture2D GetLastRender()
        {
            Texture2D tempTexture = new Texture2D(_graphics.GraphicsDevice, renderTexture.Width, renderTexture.Height);
            RenderTarget2D target = finalTexture != null ? finalTexture : renderTexture;

            target.GetData(colorData);
            tempTexture.SetData(colorData);

            return tempTexture;
        }

        public static void UpdateScreenSize(Point screenSize)
        {
            if (screenSize.X > 0 && screenSize.Y > 0)
                ScreenSize = screenSize;
        }
    }
}
