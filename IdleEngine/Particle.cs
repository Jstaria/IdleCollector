using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Simple particle for particle systems or one off text

namespace IdleEngine
{
    public delegate float Curve(float t);

    public struct ParticleStats
    {
        public Vector2 StartingVelocity;
        public Vector2 ActingForce;

        public float Lifespan;
        public string TextureKey;

        public Color StartColor;
        public Color EndColor;
        public Curve ColorDecayRate;

        public float Size;
        public Curve SizeDecayRate;

        public float RotationAngle;
        public float RotationSpeed;

        public string ParticleText;
        public SpriteFont Font;
    }

    internal class Particle
    {
        private Vector2 position;
        private Vector2 velocity;

        private float lifeSpan;

        private Texture2D asset;
        private Color startColor;
        private Color endColor;
        private float rotationSpeed;
        private float rotationAngle;
        private float colorDecay;

        private const int def = 1;
        private int speed;
        private string amount;
        private float size;
        private bool isRadial;
        private float spreadAngle;

        private SpriteFont particleFont;

        public float LifeSpan { get => lifeSpan; }

        private ParticleStats stats;

        public Particle(ParticleStats stats)
        {
            this.stats = stats;
            lifeSpan = stats.Lifespan;
        }

        public void Update(GameTime gameTime)
        {
            position += velocity;
            velocity += stats.ActingForce;

            lifeSpan -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            float t = lifeSpan / stats.Lifespan;

            size = stats.SizeDecayRate(t);

            colorDecay = stats.ColorDecayRate(t);

            rotationAngle += rotationSpeed;
        }

        public void DrawString(SpriteBatch sb)
        {
            sb.DrawString(particleFont, amount, position - particleFont.MeasureString(amount) / 2, Color.Black * LifeSpan);
        }

        public void DrawAsset(SpriteBatch sb)
        {
            sb.Draw(asset, position, null, Color.Lerp(endColor, startColor, colorDecay) * LifeSpan, rotationAngle, new Vector2(asset.Width / 2, asset.Height / 2), size, SpriteEffects.None, 0);
        }
    }
}
