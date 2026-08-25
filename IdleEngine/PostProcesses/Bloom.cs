using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace IdleEngine.PostProcesses
{
    public class Bloom : PostProcess
    {
        public struct BloomConfig
        {
            public float bloomThreshold;
            public float bloomStrength;
            public float spreadStrength;
            public Color bloomTint;
        }

        private readonly Effect effect;
        private BloomConfig config;

        private RenderTarget2D extractedBloom;
        private RenderTarget2D blurredBloom;

        // Two final buffers so we never sample from a texture
        // while simultaneously rendering into it.
        private RenderTarget2D finalBloomA;
        private RenderTarget2D finalBloomB;

        private const float BlurStepSize = 1f;
        private const int MaxSampleCount = 8;

        public bool SaveExtractedBloomDebugPng { get; set; }

        public Bloom(Effect effect, BloomConfig config)
        {
            this.effect = effect ?? throw new ArgumentNullException(nameof(effect));
            this.config = config;
        }

        public BloomConfig Config
        {
            get => config;
            set => config = value;
        }

        public override void Draw(SpriteBatch sb, ref RenderTarget2D renderTarget)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget));

            GraphicsDevice gd = sb.GraphicsDevice;

            EnsureRenderTargets(gd, renderTarget);

            // ------------------------------------------------------------
            // PARAMETERS
            // ------------------------------------------------------------

            effect.Parameters["bloomThreshold"]?.SetValue(
                MathHelper.Clamp(config.bloomThreshold, 0f, 1f));

            effect.Parameters["bloomStrength"]?.SetValue(
                Math.Max(0f, config.bloomStrength));

            effect.Parameters["bloomTint"]?.SetValue(
                config.bloomTint.ToVector3());

            effect.Parameters["sampleCount"]?.SetValue(
                (float)(int)MathHelper.Clamp(
                    config.spreadStrength,
                    0f,
                    MaxSampleCount));

            effect.Parameters["blurOffset"]?.SetValue(
                new Vector2(
                    BlurStepSize / renderTarget.Width,
                    BlurStepSize / renderTarget.Height));

            // Keep a reference to the input.
            // We MUST NOT render into this target during the combine pass.
            RenderTarget2D original = renderTarget;

            bool saveDebug = Input.IsMiddleButtonDownOnce();

            // ------------------------------------------------------------
            // 0. ORIGINAL DEBUG
            // ------------------------------------------------------------

            if (saveDebug)
                SaveRenderTargetPng(original, "original.png");

            // ------------------------------------------------------------
            // 1. EXTRACT
            // ------------------------------------------------------------

            gd.SetRenderTarget(extractedBloom);
            gd.Clear(Color.Black);

            effect.CurrentTechnique = effect.Techniques["Extract"];

            sb.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect);

            sb.Draw(
                original,
                extractedBloom.Bounds,
                Color.White);

            sb.End();

            if (saveDebug)
                SaveRenderTargetPng(
                    extractedBloom,
                    "extractedBloom.png");

            // ------------------------------------------------------------
            // 2. BLUR
            // ------------------------------------------------------------

            gd.SetRenderTarget(blurredBloom);
            gd.Clear(Color.Black);

            effect.CurrentTechnique = effect.Techniques["Blur"];

            sb.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect);

            sb.Draw(
                extractedBloom,
                blurredBloom.Bounds,
                Color.White);

            sb.End();

            if (saveDebug)
                SaveRenderTargetPng(
                    blurredBloom,
                    "blurredBloom.png");

            // ------------------------------------------------------------
            // 3. COMBINE
            //
            // originalTexture = ORIGINAL
            // textureSampler  = blurredBloom
            //
            // We render into a DIFFERENT target.
            // ------------------------------------------------------------

            effect.Parameters["originalTexture"]?.SetValue(original);

            RenderTarget2D output;

            // Pick whichever final buffer isn't the current source.
            if (ReferenceEquals(original, finalBloomA))
                output = finalBloomB;
            else
                output = finalBloomA;

            gd.SetRenderTarget(output);
            gd.Clear(Color.Black);

            effect.CurrentTechnique = effect.Techniques["Bloom"];

            sb.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect);

            sb.Draw(
                blurredBloom,
                output.Bounds,
                Color.White);

            sb.End();

            if (saveDebug)
                SaveRenderTargetPng(
                    output,
                    "combinedBloom.png");

            // ------------------------------------------------------------
            // 4. OUTPUT
            // ------------------------------------------------------------

            renderTarget = output;

            gd.SetRenderTarget(null);
        }

        private static void SaveRenderTargetPng(
            RenderTarget2D target,
            string fileName)
        {
            using (FileStream stream = File.Create(fileName))
            {
                target.SaveAsPng(
                    stream,
                    target.Width,
                    target.Height);
            }
        }

        private void EnsureRenderTargets(
            GraphicsDevice graphicsDevice,
            RenderTarget2D source)
        {
            bool valid =
                extractedBloom != null &&
                blurredBloom != null &&
                finalBloomA != null &&
                finalBloomB != null &&
                extractedBloom.Width == source.Width &&
                extractedBloom.Height == source.Height &&
                blurredBloom.Width == source.Width &&
                blurredBloom.Height == source.Height &&
                finalBloomA.Width == source.Width &&
                finalBloomA.Height == source.Height &&
                finalBloomB.Width == source.Width &&
                finalBloomB.Height == source.Height &&
                extractedBloom.Format == source.Format &&
                blurredBloom.Format == source.Format &&
                finalBloomA.Format == source.Format &&
                finalBloomB.Format == source.Format;

            if (valid)
                return;

            extractedBloom?.Dispose();
            blurredBloom?.Dispose();
            finalBloomA?.Dispose();
            finalBloomB?.Dispose();

            extractedBloom = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);

            blurredBloom = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);

            finalBloomA = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);

            finalBloomB = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);
        }
    }
}