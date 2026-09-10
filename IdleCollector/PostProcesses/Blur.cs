using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IdleCollector;
using System;
using System.Collections.Generic;

namespace IdleEngine.PostProcesses
{
    public class Blur : PostProcess
    {
        public sealed class BlurConfig
        {
            public bool useBlur = true;
            public float maskCutoff = .4f;
            public int maxBlurRadius = 8;
        }

        private static Blur instance;

        public static Blur Instance => instance ??= new Blur();

        private readonly Effect effect;
        private readonly Texture2D maskTexture;
        private RenderTarget2D outputTexture;
        private static readonly IReadOnlyCollection<string> targetScenes = new[] { "Game Scene", "Options Scene" };
        private readonly BlurConfig config = new();

        public float MaskCutoff { get => config.maskCutoff; set => config.maskCutoff = value; }
        public int MaxBlurRadius => config.maxBlurRadius;
        public override IReadOnlyCollection<string> SceneTargets => targetScenes;
        public bool UseBlur => config.useBlur;
        public BlurConfig Config => config;

        public Blur()
        {
            instance ??= this;
            effect = ResourceAtlas.GetEffect("MaskedBlur");
            maskTexture = ResourceAtlas.GetTexture("blur");
        }

        public override void Draw(
            SpriteBatch sb,
            ref RenderTarget2D normalTexture,
            RenderTarget2D uiTexture,
            ref RenderTarget2D combinedTexture)
        {
            if (!config.useBlur)
                return;

            if (sb == null)
                throw new ArgumentNullException(nameof(sb));
            if (normalTexture == null)
                throw new ArgumentNullException(nameof(normalTexture));

            EnsureOutputTexture(sb.GraphicsDevice, normalTexture);

            effect.Parameters["blurMaskTexture"]?.SetValue(maskTexture);
            effect.Parameters["texelSize"]?.SetValue(new Vector2(1f / normalTexture.Width, 1f / normalTexture.Height));
            effect.Parameters["maskCutoff"]?.SetValue(MathHelper.Clamp(MaskCutoff, 0f, 1f));
            effect.Parameters["maxBlurRadius"]?.SetValue(Math.Clamp(MaxBlurRadius, 1, 8));
            effect.CurrentTechnique = effect.Techniques["MaskedBlur"];

            sb.GraphicsDevice.SetRenderTarget(outputTexture);
            sb.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(normalTexture, outputTexture.Bounds, Color.White);
            sb.End();

            normalTexture = outputTexture;
            sb.GraphicsDevice.SetRenderTarget(null);
        }

        public void SetBlurRadius(int radius)
        {
            config.maxBlurRadius = Math.Clamp(radius, 1, 8);
            Save();
        }

        public bool Toggle()
        {
            config.useBlur = !config.useBlur;
            Save();
            return config.useBlur;
        }

        private void Save()
        {
            FileIO.WriteJsonTo(config, "Content/Config/Blur", Newtonsoft.Json.Formatting.Indented);
        }

        private void EnsureOutputTexture(GraphicsDevice graphicsDevice, RenderTarget2D source)
        {
            if (outputTexture != null && outputTexture.Width == source.Width &&
                outputTexture.Height == source.Height && outputTexture.Format == source.Format)
                return;

            outputTexture?.Dispose();
            outputTexture = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);
        }
    }
}
