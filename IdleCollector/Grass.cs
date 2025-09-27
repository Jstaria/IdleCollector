using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class Grass : Interactable
    {
        private Vector2 xOffsetAmt;
        private float rotationAmt;
        private Spring posSpring;
        private Spring rotSpring;

        private Color WaveColor;
        private Color InvWaveColor;

        public override Vector2 Origin { get => new Vector2(Bounds.Width, Bounds.Height * 2); }

        public Grass(string atlasType, string atlasKey) : base()
        {
            tileType = atlasType;
            textureKey = atlasKey;

            posSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotationAmt = MathHelper.ToRadians(45);

            textureSourceRect = ResourceAtlas.GetTileRect(tileType, textureKey);
        }

        public override void Update(GameTime gameTime)
        {
            posSpring.Update();
            rotSpring.Update();

            Rotation = rotSpring.Position * rotationAmt;
        }

        public override void Draw(SpriteBatch sb)
        {
            Vector2 offset = xOffsetAmt * posSpring.Position;
            Rectangle rect = new Rectangle(Bounds.Location + offset.ToPoint(), Bounds.Size);

            float yPos = Position.Y + offset.Y + Origin.Y + Rotation;
            LayerDepth = (yPos - WorldDepth) / (float)WorldHeight + float.Epsilon;

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, textureSourceRect, DrawColor, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }

        public override void InteractWith(ICollidable collider)
        {
            SetRotation(collider);
        }

        public override void SetRotation(ICollidable collider)
        {
            Vector2 colliderOrigin = collider.Position;
            Vector2 position = Position;
            Vector2 direction = position - colliderOrigin;
            direction.Normalize();

            float dot = Vector2.Dot(Vector2.UnitX, direction);

            float amt = 20;

            float distance = Vector2.Distance(position, colliderOrigin);
            if (distance > amt) return;

            float lerp = 1 - distance / amt;
            rotationAmt = MathF.Sign(dot) * MathHelper.ToRadians(45);
            xOffsetAmt = direction * amt * .25f; // Vector2.UnitX * MathF.Sign(dot) * amt * .75f;

            posSpring.RestPosition = lerp;
            rotSpring.RestPosition = lerp;
        }

        public override void Nudge(float strength)
        {
            rotSpring.Nudge(strength);
        }
        public override void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            if (WaveColor == Color.Transparent)
            {
                float value = .1f;
                WaveColor = new Color(Color.ToVector3() + new Vector3(value));
            }
            float noiseValue = noise.GetNoise(Position.X + windScroll.X, Position.Y + windScroll.Y);
            rotSpring.Nudge(noiseValue * .5f);

            DrawColor = Color.Lerp(WaveColor, Color, ((noiseValue + 1) / 2));
        }
    }
}
