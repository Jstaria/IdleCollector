using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    internal sealed class SkillTreeConstellationGlow : PostProcess
    {
        private readonly Effect effect;
        private readonly Action<SpriteBatch> drawMask;
        private readonly Func<float> getOpacity;
        private readonly string[] sceneTargets;
        private RenderTarget2D mask;
        private RenderTarget2D output;

        public override IReadOnlyCollection<string> SceneTargets => sceneTargets;

        public SkillTreeConstellationGlow(Action<SpriteBatch> drawMask, Func<float> getOpacity, string scene)
        {
            this.drawMask = drawMask;
            this.getOpacity = getOpacity;
            sceneTargets = new[] { scene };
            effect = ResourceAtlas.GetEffect("SkillTreeHoverHighlight");
        }

        public override void Draw(SpriteBatch sb, ref RenderTarget2D normalTexture,
            RenderTarget2D uiTexture, ref RenderTarget2D combinedTexture)
        {
            float opacity = getOpacity();
            if (opacity <= .001f)
                return;

            EnsureRenderTargets(sb.GraphicsDevice, normalTexture);

            GraphicsDevice device = sb.GraphicsDevice;
            device.SetRenderTarget(mask);
            device.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Renderer.CurrentCamera.Transform);
            drawMask(sb);
            sb.End();

            device.SetRenderTarget(output);
            device.Clear(Color.Transparent);
            effect.Parameters["hoverMask"].SetValue(mask);
            effect.Parameters["texelSize"].SetValue(new Vector2(1f / output.Width, 1f / output.Height));
            effect.Parameters["highlightStrength"].SetValue(opacity);
            effect.Parameters["blurRadius"].SetValue(2f);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(normalTexture, output.Bounds, Color.White);
            sb.End();

            normalTexture = output;
            device.SetRenderTarget(null);
        }

        private void EnsureRenderTargets(GraphicsDevice device, RenderTarget2D source)
        {
            if (output != null && output.Width == source.Width && output.Height == source.Height && output.Format == source.Format)
                return;

            output?.Dispose();
            mask?.Dispose();
            output = new RenderTarget2D(device, source.Width, source.Height, false, source.Format, DepthFormat.None);
            mask = new RenderTarget2D(device, source.Width, source.Height);
        }
    }
}
