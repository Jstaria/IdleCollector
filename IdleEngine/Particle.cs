using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Simple particle for particle systems or one off text

namespace IdleCollector
{
    internal class Particle
    {
        private Vector2 position;
        private float decayRate;

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

        public float LifeSpan { get; private set; }

        // Because it is only me using this particular particle system, I will not comment out each ad every one of these
        public Particle(Vector2 postiion, float decayRate, string amount, SpriteFont particleFont)
        {
            this.particleFont = particleFont;
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.amount = amount;
            speed = def;
            rotationSpeed = 0;
            rotationAngle = 0;
            size = 1;
        }

        public Particle(Vector2 postiion, float decayRate, Texture2D asset, Color color)
        {
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.asset = asset;
            startColor = color;
            endColor = color;
            speed = def;
            rotationSpeed = 0;
            rotationAngle = 0;
            size = 1;
        }

        public Particle(Vector2 postiion, float decayRate, Texture2D asset, Color startColor, Color endColor)
        {
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.asset = asset;
            this.startColor = startColor;
            this.endColor = endColor;
            speed = def;
            rotationSpeed = 0;
            rotationAngle = 0;
            size = 1;
        }

        public Particle(Vector2 postiion, float decayRate, Texture2D asset, Color startColor, Color endColor, int speed)
        {
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.asset = asset;
            this.startColor = startColor;
            this.endColor = endColor;
            this.speed = speed;
            rotationSpeed = 0;
            rotationAngle = 0;
            size = 1;
        }

        public Particle(Vector2 postiion, float decayRate, Texture2D asset, Color startColor, Color endColor, int speed, float rotationSpeed)
        {
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.asset = asset;
            this.startColor = startColor;
            this.endColor = endColor;
            this.speed = speed;
            this.rotationSpeed = rotationSpeed;
            rotationAngle = 0;
            size = 1;
        }

        public Particle(Vector2 postiion, float decayRate, Texture2D asset, Color startColor, Color endColor, int speed, float rotationSpeed, float spreadAngle, bool isRadial)
        {
            position = postiion;
            LifeSpan = 1;
            this.decayRate = decayRate;
            this.asset = asset;
            this.startColor = startColor;
            this.endColor = endColor;
            this.speed = speed;
            this.rotationSpeed = rotationSpeed;
            rotationAngle = 0;
            size = 1;
            this.spreadAngle = spreadAngle - 135;
            this.isRadial = isRadial;
            colorDecay = 1f;
        }

        public void Update()
        {
            if (isRadial)
            {
                position += new Vector2(
                    (float)(Math.Cos(MathHelper.ToRadians(spreadAngle)) - Math.Sin(MathHelper.ToRadians(spreadAngle))) * speed,
                    (float)(Math.Sin(MathHelper.ToRadians(spreadAngle)) + Math.Cos(MathHelper.ToRadians(spreadAngle))) * speed);
            }
            else
            {
                position.Y -= speed;
            }

            if (LifeSpan > .005f)
            {
                LifeSpan *= decayRate;
            }

            else
            {
                LifeSpan = 0;
            }

            size *= .995f;
            colorDecay *= new Random().Next(970, 985) * .001f;

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
