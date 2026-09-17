using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SkillTreeCreationTool
{
    internal sealed class LinkPulseRenderable : IRenderable
    {
        private readonly Effect effect;
        private readonly Action<SpriteBatch, Effect> drawLinks;
        private readonly Func<float> getTime;

        public float LayerDepth { get; set; }
        public Color Color { get; set; } = Color.White;

        public LinkPulseRenderable(Effect effect, Action<SpriteBatch, Effect> drawLinks, Func<float> getTime)
        {
            this.effect = effect;
            this.drawLinks = drawLinks;
            this.getTime = getTime;
        }

        public void Draw(SpriteBatch sb)
        {
            sb.End();
            effect.Parameters["iTime"]?.SetValue(getTime());
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect, Renderer.CurrentCamera.Transform);
            drawLinks(sb, effect);
            sb.End();
            Renderer.ResetBeginDraw(sb, drawSpace: DrawSpace.World);
        }
    }
}
