using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
        private RenderTarget2D combinedBloom;

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

            GraphicsDevice graphicsDevice = sb.GraphicsDevice;
            EnsureRenderTargets(graphicsDevice, renderTarget);

            effect.Parameters["bloomThreshold"].SetValue(MathHelper.Clamp(config.bloomThreshold, 0f, 1f));
            effect.Parameters["bloomStrength"].SetValue(Math.Max(0f, config.bloomStrength));
            effect.Parameters["bloomTint"].SetValue(config.bloomTint.ToVector3());

            graphicsDevice.SetRenderTarget(extractedBloom);
            graphicsDevice.Clear(Color.Transparent);
            sb.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp, effect: effect);
            effect.CurrentTechnique.Passes["EXTRACT"].Apply();
            sb.Draw(renderTarget, extractedBloom.Bounds, Color.White);
            sb.End();

            effect.Parameters["originalTexture"].SetValue(renderTarget);
            graphicsDevice.SetRenderTarget(combinedBloom);
            graphicsDevice.Clear(Color.Transparent);
            sb.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp, effect: effect);
            effect.CurrentTechnique.Passes["BLOOM"].Apply();
            sb.Draw(extractedBloom, combinedBloom.Bounds, Color.White);
            sb.End();

            renderTarget = combinedBloom;
            graphicsDevice.SetRenderTarget(null);
        }

        private void EnsureRenderTargets(GraphicsDevice graphicsDevice, RenderTarget2D source)
        {
            if (extractedBloom != null &&
                extractedBloom.Width == source.Width &&
                extractedBloom.Height == source.Height &&
                extractedBloom.Format == source.Format)
            {
                return;
            }

            extractedBloom?.Dispose();
            combinedBloom?.Dispose();

            extractedBloom = new RenderTarget2D(graphicsDevice, source.Width, source.Height, false, source.Format, DepthFormat.None);
            combinedBloom = new RenderTarget2D(graphicsDevice, source.Width, source.Height, false, source.Format, DepthFormat.None);
        }
    }
}
