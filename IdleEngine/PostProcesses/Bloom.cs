using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace IdleEngine.PostProcesses
{
    public class Bloom : PostProcess
    {
        private static Bloom instance;

        public static Bloom Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Bloom();
                }

                return instance;
            }
        }

        private static BloomConfig GetBloomConfig()
        {
            return new BloomConfig
            {
                bloomStrength = .5f,
                bloomThreshold = .45f,
                bloomTint = Color.White
            };
        }

        public struct BloomConfig
        {
            public float bloomThreshold;
            public float bloomStrength;
            public Color bloomTint;
        }

        private readonly Effect effect;
        private BloomConfig config;

        private RenderTarget2D extractedBloom;
        private RenderTarget2D downsampledBloom;
        private RenderTarget2D blurredBloom;

        private RenderTarget2D finalBloomA;
        private RenderTarget2D finalBloomB;

        private const int BloomDownsampleFactor = 4;

        public bool SaveExtractedBloomDebugPng { get; set; }

        public Bloom()
        {
            if (instance == null)
                instance = this;

            effect = ResourceAtlas.GetEffect("Bloom");
            config = GetBloomConfig();
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

            GraphicsDevice graphicsDevice = sb.GraphicsDevice;
            EnsureRenderTargets(graphicsDevice, renderTarget);
            SetEffectParameters(renderTarget);

            RenderTarget2D original = renderTarget;
            bool saveDebug = SaveExtractedBloomDebugPng || Input.IsMiddleButtonDownOnce();

            if (saveDebug)
                SaveRenderTargetPng(original, "original.png");

            RunPass(sb, graphicsDevice, original, extractedBloom, "Extract", SamplerState.LinearClamp);

            if (saveDebug)
                SaveRenderTargetPng(extractedBloom, "extractedBloom.png");

            RunPass(sb, graphicsDevice, extractedBloom, downsampledBloom, null, SamplerState.LinearClamp);
            RunPass(sb, graphicsDevice, downsampledBloom, blurredBloom, null, SamplerState.LinearClamp);

            if (saveDebug)
                SaveRenderTargetPng(blurredBloom, "blurredBloom.png");

            effect.Parameters["originalTexture"]?.SetValue(original);
            RenderTarget2D output = ReferenceEquals(original, finalBloomA) ? finalBloomB : finalBloomA;
            RunPass(sb, graphicsDevice, blurredBloom, output, "Bloom", SamplerState.LinearClamp);

            if (saveDebug)
                SaveRenderTargetPng(output, "combinedBloom.png");

            renderTarget = output;
            graphicsDevice.SetRenderTarget(null);

        }

        private void SetEffectParameters(RenderTarget2D source)
        {
            effect.Parameters["bloomThreshold"]?.SetValue(MathHelper.Clamp(config.bloomThreshold, 0f, 1f));
            effect.Parameters["bloomStrength"]?.SetValue(Math.Max(0f, config.bloomStrength));
            effect.Parameters["bloomTint"]?.SetValue(config.bloomTint.ToVector3());
        }

        private void RunPass(SpriteBatch sb, GraphicsDevice graphicsDevice, Texture2D source,
            RenderTarget2D target, string technique, SamplerState samplerState)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(Color.Black);
            if (technique != null) effect.CurrentTechnique = effect.Techniques[technique];

            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, samplerState,
                DepthStencilState.None, RasterizerState.CullNone, technique != null ? effect : null);
            sb.Draw(source, target.Bounds, Color.White);
            sb.End();
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
            if (IsMatch(extractedBloom, source) && IsDownsampleMatch(downsampledBloom, source) && IsMatch(blurredBloom, source) &&
                IsMatch(finalBloomA, source) && IsMatch(finalBloomB, source))
                return;

            extractedBloom?.Dispose();
            downsampledBloom?.Dispose();
            blurredBloom?.Dispose();
            finalBloomA?.Dispose();
            finalBloomB?.Dispose();

            extractedBloom = CreateRenderTarget(graphicsDevice, source);
            downsampledBloom = CreateDownsampleTarget(graphicsDevice, source);
            blurredBloom = CreateRenderTarget(graphicsDevice, source);
            finalBloomA = CreateRenderTarget(graphicsDevice, source);
            finalBloomB = CreateRenderTarget(graphicsDevice, source);
        }

        private static bool IsMatch(RenderTarget2D target, RenderTarget2D source) =>
            target != null && target.Width == source.Width && target.Height == source.Height && target.Format == source.Format;

        private static bool IsDownsampleMatch(RenderTarget2D target, RenderTarget2D source) =>
            target != null && target.Width == GetDownsampledDimension(source.Width) &&
            target.Height == GetDownsampledDimension(source.Height) && target.Format == source.Format;

        private static RenderTarget2D CreateRenderTarget(GraphicsDevice graphicsDevice, RenderTarget2D source) =>
            new RenderTarget2D(graphicsDevice, source.Width, source.Height, false, source.Format, DepthFormat.None);

        private static RenderTarget2D CreateDownsampleTarget(GraphicsDevice graphicsDevice, RenderTarget2D source) =>
            new RenderTarget2D(graphicsDevice, GetDownsampledDimension(source.Width),
                GetDownsampledDimension(source.Height), false, source.Format, DepthFormat.None);

        private static int GetDownsampledDimension(int dimension) =>
            Math.Max(1, (dimension + BloomDownsampleFactor - 1) / BloomDownsampleFactor);
    }
}
