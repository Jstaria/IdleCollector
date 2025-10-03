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
        public Vector2 Position;
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

    internal class Particle: IRenderable, IUpdatable
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
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private ParticleStats stats;

        public Particle(ParticleStats stats)
        {
            this.stats = stats;
            position = stats.Position;
            lifeSpan = stats.Lifespan;
        }

        public void Update(GameTime gameTime)
        {

        }

        public void Draw(SpriteBatch sb)
        {
            if (stats.Font != null)
                DrawString(sb);
            else 
                DrawAsset(sb);
        }

        private void DrawString(SpriteBatch sb)
        {
            sb.DrawString(particleFont, amount, position - particleFont.MeasureString(amount) / 2, Color.Black * LifeSpan);
        }

        private void DrawAsset(SpriteBatch sb)
        {
            sb.Draw(asset, position, null, Color.Lerp(endColor, startColor, colorDecay) * LifeSpan, rotationAngle, new Vector2(asset.Width / 2, asset.Height / 2), size, SpriteEffects.None, 0);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            lifeSpan -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void StandardUpdate(GameTime gameTime)
        {
            position += velocity;
            velocity += stats.ActingForce;

            float t = lifeSpan / stats.Lifespan;

            size = stats.SizeDecayRate(t);

            colorDecay = stats.ColorDecayRate(t);

            rotationAngle += rotationSpeed;
        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }
    }
}
