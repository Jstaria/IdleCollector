using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SkillTreeCreationTool
{
    internal sealed class TokenUnlockWisp
    {
        public const float BurnDuration = 1.25f;
        public const float FadeDuration = .35f;
        private const float VisualFadeDuration = .8f;
        private readonly Vector2 worldPosition;
        private readonly float radius;
        private float age;
        private float dustTimer;
        private readonly ParticleSystem dust;

        public bool Finished => age >= BurnDuration + FadeDuration;

        public TokenUnlockWisp(Vector2 worldPosition, float radius)
        {
            this.worldPosition = worldPosition;
            this.radius = radius;
            dust = new ParticleSystem(new ParticleSystemStats
            {
                SpawnBounds = new[] { new[] { new Rectangle(-(int)(radius * .7f), -2, Math.Max(1, (int)(radius * 1.4f)), 4) } },
                SpawnShape = ParticleSpawnShape.Rectangle,
                SpawnPlacement = ParticleSpawnPlacement.Inside,
                TrackPosition = () => worldPosition * RendererZoom,
                TrackLayerDepth = () => .68f,
                ParticleLifeSpan = new[] { .175f, .325f },
                ParticleDespawnDistance = 1000f,
                MaxParticleCount = 32,
                ParticleStartColor = new[] { Color.White * .25f, Color.White * .65f },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleTextureKeys = new[] { "square" },
                ParticleSize = new[] { .03f, .06f },
                ParticleSpeed = new[] { .4f, .8f },
                TrackStartingVelocity = () => new Vector2(RandomHelper.Instance.GetFloat(-.15f, .15f), -.5f) * RendererZoom,
                ActingForce = _ => new Vector2(0f, -.006f) * RendererZoom,
                ParticleRotation = new[] { 0f },
                ParticleRotationSpeed = _ => 0f,
                ParticleColorDecayRate = t => t,
                ParticleSizeDecayRate = t => (1f - t * .5f) * RendererZoom,
                EmitRate = new[] { 0f },
                EmitCount = new[] { 1 },
                ResetParticlesAfterDeath = false
            });
        }

        private float RendererZoom = 1f;

        public void Update(GameTime gameTime, float zoom)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            age += elapsedSeconds;
            RendererZoom = zoom;
            dust.SetBoundsSize(zoom);
            dustTimer += elapsedSeconds;
            while (age < BurnDuration && dustTimer >= .075f)
            {
                dust.EmitParticles();
                dustTimer -= .075f;
            }
            dust.StandardUpdate(gameTime);
        }

        public void ControlledUpdate(GameTime gameTime) => dust.ControlledUpdate(gameTime);
        public void DrawDust(SpriteBatch sb) => dust.Draw(sb);

        public void Draw(SpriteBatch sb, Effect effect, float zoom)
        {
            effect.Parameters["iTime"].SetValue(age);
            float alpha = MathHelper.Clamp(age / .1f, 0f, 1f) *
                (1f - MathHelper.SmoothStep(0f, 1f,
                    (age - (BurnDuration + FadeDuration - VisualFadeDuration)) / VisualFadeDuration));
            Vector2 size = new Vector2(radius * 2.3f, radius * 1.84f) * zoom;
            Vector2 basePosition = worldPosition * zoom;
            Vector2 position = basePosition - new Vector2(size.X / 2f, size.Y * .92f);
            sb.Draw(Drawing.Pixel, position, null, Color.White * alpha, 0f, Vector2.Zero, size, SpriteEffects.None, .69f);
        }
    }
}
