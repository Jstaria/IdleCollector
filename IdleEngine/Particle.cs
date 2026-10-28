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
    public struct ParticleStats
    {
        public GetVector Position;
        public Vector2 StartingVelocity;
        public Curve<Vector2> ActingForce;
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
        public Curve<float> RotationSpeed;
        public TrailInfo? Trail;

        public string ParticleText;
        public SpriteFont Font;
        public Color TextShadowColor;
        public Vector2? TextShadowOffset;
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
        private Trail trail;
        private Color currentColor;
        private readonly CustomText text;

        public float LifeSpan { get => lifeSpan; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private ParticleStats stats;

        public Particle(ParticleStats stats)
        {
            this.stats = stats;

            if (stats.TextureKey != null && stats.TextureKey != "")
                texture = ResourceAtlas.GetTexture(stats.TextureKey);

            if (stats.Font != null && stats.ParticleText != null)
            {
                Vector2 dimension = Vector2.Max(Vector2.One, stats.Font.MeasureString(stats.ParticleText));
                text = new CustomText(stats.Font, stats.ParticleText, Vector2.Zero, dimension, allowOverflow: true)
                {
                    CenterText = true,
                    ShadowColor = stats.TextShadowColor,
                    ShadowOffset = stats.TextShadowOffset ?? new Vector2(-4f, 4f)
                };
                text.Refresh();
            }
            
            Reset();
        }

        public void Draw(SpriteBatch sb)
        {
            trail?.Draw(sb);

            if (texture != null)
                DrawAsset(sb);
            if (text != null)
                DrawString(sb);
        }

        private void DrawString(SpriteBatch sb)
        {
            text.Color = currentColor;
            text.ShadowColor = stats.TextShadowColor * (currentColor.A / 255f);
            text.LayerDepth = stats.LayerDepth?.Invoke() ?? LayerDepth;
            text.Draw(sb, position, rotationAngle, size);
        }

        private void DrawAsset(SpriteBatch sb)
        {
            sb.Draw(texture, position, null, currentColor, rotationAngle, new Vector2(texture.Width / 2.0f, texture.Height / 2.0f), size, SpriteEffects.None, stats.LayerDepth.Invoke());
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            lifeSpan -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            float t = lifeSpan / stats.Lifespan;

            if (stats.ActingForce != null)
                velocity += stats.ActingForce.Invoke(t);

            position += velocity * stats.Speed;
            trail?.ControlledUpdate(gameTime);
        }

       public void StandardUpdate(GameTime gameTime)
        {
            float t = 1 - lifeSpan / stats.Lifespan;

            size = (stats.SizeDecayRate?.Invoke(t) ?? 1f) * stats.Size;

            colorDecay = stats.ColorDecayRate?.Invoke(t) ?? t;
            currentColor = Color.Lerp(stats.StartColor, stats.EndColor, colorDecay);

            rotationAngle += stats.RotationSpeed?.Invoke(t) ?? 0f;
            text?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            trail?.StandardUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            trail?.SlowUpdate(gameTime);
        }

        public void Reset()
        {
            lifeSpan = stats.Lifespan;
            position = stats.Position.Invoke();
            velocity = stats.StartingVelocity;
            rotationAngle = stats.Rotation;
            currentColor = stats.StartColor;
            size = stats.Size;
            CreateTrail();
        }

        private void CreateTrail()
        {
            if (!stats.Trail.HasValue)
            {
                trail = null;
                return;
            }

            TrailInfo trailInfo = stats.Trail.Value;
            trailInfo.TrackPosition = () => position;
            trailInfo.SegmentColor = _ => currentColor;
            trail = new Trail(trailInfo);
        }
        public void SetStartingVelocity(Vector2 vec) => stats.StartingVelocity = vec;
        public void SetVelocity(Vector2 vec) => velocity = vec;
    }
}
