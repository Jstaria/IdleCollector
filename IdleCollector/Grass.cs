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
        private Color WaveColor;
        private Color InvWaveColor;

        public override Vector2 Origin { get => new Vector2(Bounds.Width / 2, Bounds.Height / 2); }

        public Grass() : base()
        {
            tileType = "grass";
            textureKey =  ResourceAtlas.GetRandomAtlasKey("grass");

            posSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.5f, /*Resting Position*/0);
            rotSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotationAmt = MathHelper.ToRadians(45);
            xOffsetAmt = RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One);

            textureSourceRect = ResourceAtlas.GetTileRect(tileType, textureKey);
        }

        public override void StandardUpdate(GameTime gameTime)
        {
            posSpring.Update();
            rotSpring.Update();

            Rotation = rotSpring.Position * rotationAmt;
        }

        public override void ControlledUpdate(GameTime gameTime) 
        {
        }
        public override void SlowUpdate(GameTime gameTime)
        {
        }

        public override void Draw(SpriteBatch sb)
        {
            Vector2 offset = xOffsetAmt * posSpring.Position;
            Rectangle rect = new Rectangle(Bounds.Location + offset.ToPoint(), Bounds.Size);

            float yPos = Position.Y + offset.Y + Origin.Y * 2 + Rotation;
            LayerDepth = WorldManager.GetLayerDepth(yPos);

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, textureSourceRect, DrawColor, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }

        public override void InteractWith(Entity collider)
        {
            SetRotation(collider, 25, .5f, true);
        }

        public override void Nudge(float strength)
        {
            posSpring.Nudge(strength);
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
            rotSpring.Nudge(noiseValue);

            DrawColor = Color.Lerp(WaveColor, Color, ((noiseValue + 1) / 2));
        }
    }
}
