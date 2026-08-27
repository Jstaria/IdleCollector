using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
                bloomStrength = .60f,
                bloomThreshold = .15f,
                bloomTint = Color.White,
                bloomDownsampleFactor = 4,
                blurKernelRadius = 4,

                bloomExcludedColors = new(),
                bloomExclusionTolerance = .95f,
                bloomExclusionSoftness = .02f,
                bloomExclusionStrength = .4f,

                bloomBoostColors = new(),
                bloomBoostTolerance = .85f,
                bloomBoostSoftness = .02f,
                bloomBoostStrength = 2f
            };
        }

        public struct BloomConfig
        {
            public float bloomThreshold;
            public float bloomStrength;
            public Color bloomTint;
            public int bloomDownsampleFactor;
            public int blurKernelRadius;
            public List<Color> bloomExcludedColors;
            public float bloomExclusionTolerance;
            public float bloomExclusionSoftness;
            public float bloomExclusionStrength;
            public List<Color> bloomBoostColors;
            public float bloomBoostTolerance;
            public float bloomBoostSoftness;
            public float bloomBoostStrength;
        }

        private readonly Effect effect;
        private BloomConfig config;

        private RenderTarget2D extractedBloom;
        private RenderTarget2D downsampledBloom;
        private RenderTarget2D blurredDownsampledBloom;

        private RenderTarget2D finalBloomA;
        private RenderTarget2D finalBloomB;

        private const int MaxBloomDownsampleFactor = 16;
        private const int MaxBlurKernelRadius = 4;
        private const int MaxExcludedBloomColors = 4;
        private readonly Vector3[] excludedBloomColors = new Vector3[MaxExcludedBloomColors];
        private const int MaxBoostBloomColors = 4;
        private readonly Vector3[] boostBloomColors = new Vector3[MaxBoostBloomColors];

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
            // dreturn;
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

            RunPass(sb, graphicsDevice, extractedBloom, downsampledBloom, "Downsample", SamplerState.LinearClamp);
            SetBlurTexelSize(downsampledBloom);
            RunPass(sb, graphicsDevice, downsampledBloom, blurredDownsampledBloom, "Blur", SamplerState.LinearClamp);

            if (saveDebug)
                SaveRenderTargetPng(blurredDownsampledBloom, "blurredBloom.png");

            effect.Parameters["originalTexture"]?.SetValue(original);
            RenderTarget2D output = ReferenceEquals(original, finalBloomA) ? finalBloomB : finalBloomA;
            RunPass(sb, graphicsDevice, blurredDownsampledBloom, output, "Bloom", SamplerState.LinearClamp);

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
            effect.Parameters["blurKernelRadius"]?.SetValue(
                Math.Clamp(config.blurKernelRadius, 0, MaxBlurKernelRadius));
            effect.Parameters["downsampleTexelSize"]?.SetValue(new Vector2(1f / source.Width, 1f / source.Height));

            Array.Clear(excludedBloomColors, 0, excludedBloomColors.Length);
            List<Color> configuredExcludedColors = config.bloomExcludedColors;
            int excludedColorCount = configuredExcludedColors == null
                ? 0
                : Math.Min(configuredExcludedColors.Count, MaxExcludedBloomColors);

            for (int i = 0; i < excludedColorCount; i++)
                excludedBloomColors[i] = configuredExcludedColors[i].ToVector3();

            effect.Parameters["bloomExcludedColors"]?.SetValue(excludedBloomColors);
            effect.Parameters["bloomExcludedColorCount"]?.SetValue(excludedColorCount);
            effect.Parameters["bloomExclusionTolerance"]?.SetValue(
                MathHelper.Clamp(config.bloomExclusionTolerance, -1f, 1f));
            effect.Parameters["bloomExclusionSoftness"]?.SetValue(
                Math.Max(0.0001f, config.bloomExclusionSoftness));
            effect.Parameters["bloomExclusionStrength"]?.SetValue(
                MathHelper.Clamp(config.bloomExclusionStrength, 0f, 1f));

            Array.Clear(boostBloomColors, 0, boostBloomColors.Length);
            List<Color> configuredBoostColors = config.bloomBoostColors;
            int boostColorCount = configuredBoostColors == null
                ? 0
                : Math.Min(configuredBoostColors.Count, MaxBoostBloomColors);

            for (int i = 0; i < boostColorCount; i++)
                boostBloomColors[i] = configuredBoostColors[i].ToVector3();

            effect.Parameters["bloomBoostColors"]?.SetValue(boostBloomColors);
            effect.Parameters["bloomBoostColorCount"]?.SetValue(boostColorCount);
            effect.Parameters["bloomBoostTolerance"]?.SetValue(
                MathHelper.Clamp(config.bloomBoostTolerance, -1f, 1f));
            effect.Parameters["bloomBoostSoftness"]?.SetValue(
                Math.Max(0.0001f, config.bloomBoostSoftness));
            effect.Parameters["bloomBoostStrength"]?.SetValue(
                Math.Max(0f, config.bloomBoostStrength));
        }

        private void SetBlurTexelSize(Texture2D source)
        {
            effect.Parameters["blurTexelSize"]?.SetValue(new Vector2(1f / source.Width, 1f / source.Height));
        }

        private void RunPass(SpriteBatch sb, GraphicsDevice graphicsDevice, Texture2D source,
            RenderTarget2D target, string technique, SamplerState samplerState)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(Color.Transparent);
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
            if (IsMatch(extractedBloom, source) && IsDownsampleMatch(downsampledBloom, source) &&
                IsDownsampleMatch(blurredDownsampledBloom, source) &&
                IsMatch(finalBloomA, source) && IsMatch(finalBloomB, source))
                return;

            extractedBloom?.Dispose();
            downsampledBloom?.Dispose();
            blurredDownsampledBloom?.Dispose();
            finalBloomA?.Dispose();
            finalBloomB?.Dispose();

            extractedBloom = CreateRenderTarget(graphicsDevice, source);
            downsampledBloom = CreateDownsampleTarget(graphicsDevice, source);
            blurredDownsampledBloom = CreateDownsampleTarget(graphicsDevice, source);
            finalBloomA = CreateRenderTarget(graphicsDevice, source);
            finalBloomB = CreateRenderTarget(graphicsDevice, source);
        }

        private static bool IsMatch(RenderTarget2D target, RenderTarget2D source) =>
            target != null && target.Width == source.Width && target.Height == source.Height && target.Format == source.Format;

        private bool IsDownsampleMatch(RenderTarget2D target, RenderTarget2D source) =>
            target != null && target.Width == GetDownsampledDimension(source.Width) &&
            target.Height == GetDownsampledDimension(source.Height) && target.Format == source.Format;

        private static RenderTarget2D CreateRenderTarget(GraphicsDevice graphicsDevice, RenderTarget2D source) =>
            new RenderTarget2D(graphicsDevice, source.Width, source.Height, false, source.Format, DepthFormat.None);

        private RenderTarget2D CreateDownsampleTarget(GraphicsDevice graphicsDevice, RenderTarget2D source) =>
            new RenderTarget2D(graphicsDevice, GetDownsampledDimension(source.Width),
                GetDownsampledDimension(source.Height), false, source.Format, DepthFormat.None);

        private int GetDownsampledDimension(int dimension)
        {
            int factor = Math.Clamp(config.bloomDownsampleFactor, 1, MaxBloomDownsampleFactor);
            return Math.Max(1, (dimension + factor - 1) / factor);
        }

        public void AddExclusionColor(Color color)
        {
            config.bloomExcludedColors ??= new List<Color>();

            if (config.bloomExcludedColors.Count < MaxExcludedBloomColors &&
                !config.bloomExcludedColors.Contains(color))
                config.bloomExcludedColors.Add(color);
        }

        public void AddBoostColor(Color color)
        {
            config.bloomBoostColors ??= new List<Color>();

            if (config.bloomBoostColors.Count < MaxBoostBloomColors &&
                !config.bloomBoostColors.Contains(color))
                config.bloomBoostColors.Add(color);
        }
    }
}
