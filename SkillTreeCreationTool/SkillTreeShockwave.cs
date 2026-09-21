using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    internal sealed class SkillTreeShockwave : PostProcess
    {
        private const int MaxShockwaves = 8;
        private static readonly IReadOnlyCollection<string> targetScenes = new[] { "SkillTreeScene" };

        private readonly Effect effect;
        private readonly List<Shockwave> shockwaves = new();
        private readonly Vector4[] shaderShockwaves = new Vector4[MaxShockwaves];
        private RenderTarget2D outputTexture;

        public float Time { get; set; }
        public override IReadOnlyCollection<string> SceneTargets => targetScenes;

        public SkillTreeShockwave()
        {
            effect = ResourceAtlas.GetEffect("SkillTreeShockwave");
        }

        public void Add(Vector2 worldPosition)
        {
            Vector2 renderPosition = Vector2.Transform(worldPosition, Renderer.CurrentCamera.Transform);
            Vector2 uv = renderPosition / Renderer.RenderSize.ToVector2();
            shockwaves.Add(new Shockwave(uv, Time));

            if (shockwaves.Count > MaxShockwaves)
                shockwaves.RemoveAt(0);
        }

        public override void Draw(
            SpriteBatch sb,
            ref RenderTarget2D normalTexture,
            RenderTarget2D uiTexture,
            ref RenderTarget2D combinedTexture)
        {
            shockwaves.RemoveAll(shockwave => Time - shockwave.StartTime > Shockwave.Duration);
            if (shockwaves.Count == 0)
                return;

            EnsureOutputTexture(sb.GraphicsDevice, normalTexture);
            Array.Clear(shaderShockwaves, 0, shaderShockwaves.Length);
            for (int i = 0; i < shockwaves.Count; i++)
                shaderShockwaves[i] = new Vector4(shockwaves[i].Position, shockwaves[i].StartTime, Shockwave.Duration);

            effect.Parameters["iTime"]?.SetValue(Time);
            effect.Parameters["shockwaves"]?.SetValue(shaderShockwaves);
            effect.Parameters["shockwaveCount"]?.SetValue(shockwaves.Count);

            sb.GraphicsDevice.SetRenderTarget(outputTexture);
            sb.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(normalTexture, outputTexture.Bounds, Color.White);
            sb.End();

            normalTexture = outputTexture;
            sb.GraphicsDevice.SetRenderTarget(null);
        }

        private void EnsureOutputTexture(GraphicsDevice graphicsDevice, RenderTarget2D source)
        {
            if (outputTexture != null && outputTexture.Width == source.Width &&
                outputTexture.Height == source.Height && outputTexture.Format == source.Format)
                return;

            outputTexture?.Dispose();
            outputTexture = new RenderTarget2D(graphicsDevice, source.Width, source.Height, false, source.Format, DepthFormat.None);
        }

        private readonly struct Shockwave
        {
            public const float Duration = .6f;
            public readonly Vector2 Position;
            public readonly float StartTime;

            public Shockwave(Vector2 position, float startTime)
            {
                Position = position;
                StartTime = startTime;
            }
        }
    }
}
