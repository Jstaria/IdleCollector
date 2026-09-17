using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    public sealed class LinkParticleSettings
    {
        public int MaxParticleCount { get; set; } = 30;
        public float EmitInterval { get; set; } = .25f;
        public float MinLifetime { get; set; } = 5f;
        public float MaxLifetime { get; set; } = 10.4f;
        public float MinFrequency { get; set; } = 1.5f;
        public float MaxFrequency { get; set; } = 5f;
        public float MinAmplitude { get; set; } = 2f;
        public float MaxAmplitude { get; set; } = 7f;
        public float MinSize { get; set; } = .75f;
        public float MaxSize { get; set; } = 1.5f;
        public float LayerDepth { get; set; } = .25f;
    }

    internal sealed class LinkParticleSystem
    {
        private readonly List<LinkParticle> particles = new();
        private readonly Random random = new();
        private readonly LinkParticleSettings settings;
        private Vector2 start;
        private Vector2 end;
        private Color color;
        private float emitTimer;

        public LinkParticleSystem(LinkParticleSettings settings)
        {
            this.settings = settings;
        }

        public void Update(GameTime gameTime, Vector2 start, Vector2 end, Color color)
        {
            this.start = start;
            this.end = end;
            this.color = color;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                LinkParticle particle = particles[i];
                particle.Progress += elapsed / particle.Lifetime;
                if (particle.Progress >= 1f)
                    particles.RemoveAt(i);
                else
                    particles[i] = particle;
            }

            emitTimer -= elapsed;
            while (emitTimer <= 0f && particles.Count < settings.MaxParticleCount)
            {
                particles.Add(new LinkParticle(random, settings));
                emitTimer += Math.Max(.001f, settings.EmitInterval);
            }
        }

        public void Draw(SpriteBatch sb, float zoom)
        {
            Vector2 path = end - start;
            if (path == Vector2.Zero)
                return;

            Vector2 sideways = Vector2.Normalize(new Vector2(-path.Y, path.X));
            foreach (LinkParticle particle in particles)
            {
                float fade = MathF.Sin(particle.Progress * MathHelper.Pi);
                float wave = MathF.Sin(particle.Progress * MathHelper.TwoPi * particle.Frequency + particle.Phase);
                Vector2 position = Vector2.Lerp(start, end, particle.Progress) + sideways * wave * particle.Amplitude * zoom * fade;
                sb.Draw(Drawing.Pixel, position, null, color * (fade * particle.Alpha), 0f, Vector2.Zero, particle.Size * zoom, SpriteEffects.None, settings.LayerDepth);
            }
        }
    }

    internal struct LinkParticle
    {
        public float Progress;
        public readonly float Lifetime;
        public readonly float Phase;
        public readonly float Frequency;
        public readonly float Amplitude;
        public readonly float Size;
        public readonly float Alpha;

        public LinkParticle(Random random, LinkParticleSettings settings)
        {
            Progress = 0f;
            Lifetime = RandomRange(random, settings.MinLifetime, settings.MaxLifetime);
            Phase = (float)random.NextDouble() * MathHelper.TwoPi;
            Frequency = RandomRange(random, settings.MinFrequency, settings.MaxFrequency);
            Amplitude = RandomRange(random, settings.MinAmplitude, settings.MaxAmplitude);
            Size = RandomRange(random, settings.MinSize, settings.MaxSize);
            Alpha = .35f + (float)random.NextDouble() * .65f;
        }

        private static float RandomRange(Random random, float min, float max) =>
            MathHelper.Lerp(Math.Min(min, max), Math.Max(min, max), (float)random.NextDouble());
    }
}
