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
        private Spring xSpring;
        private Spring ySpring;

        public Grass(string atlasType, string atlasKey) : base()
        {
            tileType = atlasType;
            textureKey = atlasKey;

            xSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            ySpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);

            textureSourceRect = ResourceAtlas.GetTileRect(tileType, textureKey);
        }

        public override void Update(GameTime gameTime)
        {
            xSpring.Update();

            Rotation = xSpring.Position * rotationAmt;
        }

        public override void Draw(SpriteBatch sb)
        {
            Rectangle rect = new Rectangle(Bounds.Location + (xOffsetAmt * xSpring.Position).ToPoint(), Bounds.Size);

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, textureSourceRect, Color, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }

        public override void InteractWith(ICollidable collider)
        {
            SetRotation(collider);
        }

        public override void SetRotation(ICollidable collider)
        {
            Vector2 colliderOrigin = collider.Position;
            Vector2 position = Position;
            Vector2 direction = colliderOrigin - position;

            float dot = Vector2.Dot(Vector2.UnitX, direction);

            float amt = 20;

            float distance = Vector2.Distance(position, colliderOrigin);
            if (distance > amt) return;

            float lerp = 1 - distance / amt;
            rotationAmt = -MathF.Sign(dot) * MathHelper.ToRadians(45);
            xOffsetAmt = Vector2.UnitX * MathF.Sign(-dot) * amt * .75f;

            xSpring.RestPosition = lerp;
        }
    }
}
