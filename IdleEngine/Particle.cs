using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

// Simple particle for particle systems or one off text

namespace IdleEngine
{
    public delegate float Curve(float t);

    public struct ParticleStats
    {
        public GetVector Position;
        public Vector2 StartingVelocity;
        public GetVector ActingForce;
        public float Speed;

        public float Lifespan;
        public GetFloat LayerDepth;
        public string TextureKey;

        public Color StartColor;
        public Color EndColor;
        public Curve ColorDecayRate;

        public float Size;
        public Curve SizeDecayRate;

        public float Rotation;
        public float RotationSpeed;

        public string ParticleText;
        public SpriteFont Font;
    }

    internal class Particle: IRenderable, IUpdatable
    {
        public Vector2 position;
        private Vector2 velocity;

        private float lifeSpan;
        private float colorDecay;
        private Texture2D texture;

        private float size;
        private float rotationAngle;

        public float LifeSpan { get => lifeSpan; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private ParticleStats stats;

        public Particle(ParticleStats stats)
        {
            this.stats = stats;

            if (stats.TextureKey != null && stats.TextureKey != "")
                texture = ResourceAtlas.GetTexture(stats.TextureKey);
            
            Reset();
        }

        public void Draw(SpriteBatch sb)
        {
            if (stats.Font == null)
                DrawAsset(sb); 
            else if (texture == null)
                DrawString(sb);
            else
            {
                DrawAsset(sb);
                DrawString(sb);
            }
        }

        private void DrawString(SpriteBatch sb)
        {
            sb.DrawString(stats.Font, stats.ParticleText, position, Color.Black * LifeSpan, 0, stats.Font.MeasureString(stats.ParticleText) / 2, size, SpriteEffects.None, stats.LayerDepth.Invoke());
        }

        private void DrawAsset(SpriteBatch sb)
        {
            
            sb.Draw(texture, position, null, Color.Lerp(stats.StartColor, stats.EndColor, colorDecay), rotationAngle, new Vector2(texture.Width / 2.0f, texture.Height / 2.0f), size, SpriteEffects.None, stats.LayerDepth.Invoke());
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            lifeSpan -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (stats.ActingForce != null)
                velocity += stats.ActingForce.Invoke();

            position += velocity * stats.Speed;
        }

       public void StandardUpdate(GameTime gameTime)
        {
            float t = 1 - lifeSpan / stats.Lifespan;

            size = stats.SizeDecayRate(t) * stats.Size;

            colorDecay = stats.ColorDecayRate(t);

            rotationAngle += stats.RotationSpeed;
        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }

        public void Reset()
        {
            lifeSpan = stats.Lifespan;
            position = stats.Position.Invoke();
            velocity = stats.StartingVelocity;
            rotationAngle = stats.Rotation;
        }
        public void SetStartingVelocity(Vector2 vec) => stats.StartingVelocity = vec;
        public void SetVelocity(Vector2 vec) => velocity = vec;
    }
}
