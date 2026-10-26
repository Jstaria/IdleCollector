using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    internal sealed class SkillTreeHoverHighlight : PostProcess
    {
        private readonly Effect effect;
        private readonly List<HoverHighlight> highlights = new();
        private readonly Func<SkillTreeToken, Vector2> getPosition;
        private readonly Func<float> getZoom;
        private readonly string[] sceneTargets;
        private RenderTarget2D mask;
        private RenderTarget2D output;

        public override IReadOnlyCollection<string> SceneTargets => sceneTargets;

        public SkillTreeHoverHighlight(Func<SkillTreeToken, Vector2> getPosition,
            Func<float> getZoom, string scene)
        {
            this.getPosition = getPosition;
            this.getZoom = getZoom;
            sceneTargets = new[] { scene };
            effect = ResourceAtlas.GetEffect("SkillTreeHoverHighlight");
        }

        public void Update(SkillTreeToken hoveredToken, SkillTreeToken selectedToken, float elapsedSeconds)
        {
            EnsureHighlight(hoveredToken);
            EnsureHighlight(selectedToken);

            float blend = 1f - MathF.Exp(-5f * elapsedSeconds);
            for (int i = highlights.Count - 1; i >= 0; i--)
            {
                HoverHighlight item = highlights[i];
                bool shouldHighlight = item.Token == hoveredToken || item.Token == selectedToken;
                item.Alpha = MathHelper.Lerp(item.Alpha, shouldHighlight ? 1f : 0f, blend);
                if (!shouldHighlight && item.Alpha <= .01f)
                    highlights.RemoveAt(i);
            }
        }

        private void EnsureHighlight(SkillTreeToken token)
        {
            if (token != null && !highlights.Exists(item => item.Token == token))
            {
                if (highlights.Count == 3)
                {
                    HoverHighlight weakest = highlights[0];
                    foreach (HoverHighlight item in highlights)
                        if (item.Alpha < weakest.Alpha)
                            weakest = item;
                    highlights.Remove(weakest);
                }
                highlights.Add(new HoverHighlight(token));
            }
        }

        private sealed class HoverHighlight
        {
            public readonly SkillTreeToken Token;
            public float Alpha;
            public HoverHighlight(SkillTreeToken token) => Token = token;
        }

        public override void Draw(SpriteBatch sb, ref RenderTarget2D normalTexture,
            RenderTarget2D uiTexture, ref RenderTarget2D combinedTexture)
        {
            float zoom = getZoom();
            if (highlights.Count == 0)
                return;

            GraphicsDevice device = sb.GraphicsDevice;
            if (output == null || output.Width != normalTexture.Width || output.Height != normalTexture.Height || output.Format != normalTexture.Format)
            {
                output?.Dispose();
                mask?.Dispose();
                output = new RenderTarget2D(device, normalTexture.Width, normalTexture.Height, false, normalTexture.Format, DepthFormat.None);
                mask = new RenderTarget2D(device, normalTexture.Width, normalTexture.Height);
            }

            device.SetRenderTarget(mask);
            device.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Renderer.CurrentCamera.Transform);
            foreach (HoverHighlight item in highlights)
                item.Token.DrawIcon(sb, getPosition(item.Token), zoom, highlightMask: true, opacity: item.Alpha);
            sb.End();

            device.SetRenderTarget(output);
            device.Clear(Color.Transparent);
            effect.Parameters["hoverMask"].SetValue(mask);
            effect.Parameters["texelSize"].SetValue(new Vector2(1f / output.Width, 1f / output.Height));
            effect.Parameters["highlightStrength"].SetValue(1f);
            effect.Parameters["blurRadius"].SetValue(1.5f * zoom);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(normalTexture, output.Bounds, Color.White);
            sb.End();
            normalTexture = output;
            device.SetRenderTarget(null);
        }
    }
}
