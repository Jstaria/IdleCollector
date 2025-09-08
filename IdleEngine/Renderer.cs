using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particles;
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
        public static Dictionary<string, OnDraw> DrawEvents;
        private static event OnDraw DrawEvent;
        private static event OnDraw IndependentDrawEvent;

        private static RenderTarget2D renderTexture;
        private static RenderTarget2D finalTexture;
        private static RenderTarget2D[] targets;
        private static Color[] colorData;

        private static BatchConfig renderTexConfig;

        private static List<BatchConfig> processes;

        public static void Initialize(GraphicsDevice GraphicsDevice)
        {
            DrawEvents = new();

            processes = new List<BatchConfig>();

            renderTexture = new RenderTarget2D(GraphicsDevice, 240, 135);
            renderTexConfig = new BatchConfig(
                SpriteSortMode.Immediate,
                null,
                SamplerState.PointClamp,
                null, null, null, null,
                Matrix.Identity
                );

            targets = new RenderTarget2D[] {
                new RenderTarget2D(GraphicsDevice, renderTexture.Width, renderTexture.Height),
                new RenderTarget2D(GraphicsDevice, renderTexture.Width, renderTexture.Height) };

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
                transformMatrix: renderTexConfig.transformMatrix
                );
            DrawEvent?.Invoke(sb);
            IndependentDrawEvent?.Invoke(sb);
            sb.End();

            // Idea of another set of draw calls for primitives not using spritebatch
            //ShapeBatch.BeginTextured(sb.GraphicsDevice, squareTex, renderTexture);

            //ShapeBatch.BoxTextured(0, 0, 16*32, 16*32);

            //ShapeBatch.End();

            sb.GraphicsDevice.SetRenderTarget(null);

            for (int i = 0; i < processes.Count; i++)
            {
                ApplyEffectValues(processes[i], sb);
            }

            if (processes.Count == 0)
                finalTexture = renderTexture;
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
            sb.Draw(finalTexture, destinationRect, Color.White);
            sb.End();
        }

        internal static void SwapScene(string sceneName)
        {
            DrawEvent = DrawEvents[sceneName];
        }

        internal static void AddScene(string sceneName)
        {
            if (DrawEvents.ContainsKey(sceneName))
                throw new Exception(String.Format("Events already has scene: {0}", sceneName));
            DrawEvents.Add(sceneName, (SpriteBatch sb) => { });
        }

        /// <summary>
        /// Adds to a scene's draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneDraw(string sceneName, IDrawable drawable) => AddToSceneDraw(sceneName, drawable.Draw);
        /// <summary>
        /// Adds to a scene's draw loop, must swap scene to see effects
        /// </summary>
        public static void AddToSceneDraw(string sceneName, OnDraw func) => DrawEvents[sceneName] += func;
        /// <summary>
        /// Adds to current scene's draw loop, does not require scene swap
        /// </summary>
        public static void AddToSceneDraw(IDrawable drawable) => AddToSceneDraw(drawable.Draw);
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
        public static void AddToDraw(IDrawable drawable) => AddToDraw(drawable.Draw);
        /// <summary>
        /// Adds to scene independent draw loop, does not require scene swap
        /// </summary>
        public static void AddToDraw(OnDraw func) => IndependentDrawEvent += func;
    }
}
