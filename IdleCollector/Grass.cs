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
        private Vector2 offset;

        public Grass(string atlasType, string atlasKey) : base()
        {
            tileType = atlasType;
            textureKey = atlasKey;

            textureSourceRect = ResourceAtlas.GetTileRect(tileType, textureKey);
        }

        public override void Update(GameTime gameTime)
        {

        }

        public override void Draw(SpriteBatch sb)
        {
            Rectangle rect = new Rectangle(Bounds.Location + offset.ToPoint(), Bounds.Size);

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, textureSourceRect, Color, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }

        public override void InteractWith(ICollidable collider)
        {
            
        }

        public override void SetRotation(ICollidable collider, int tileOriginX)
        {
            Vector2 colliderOrigin = collider.Position;
            Vector2 position = Position;
            Vector2 direction = colliderOrigin - position;

            float dot = Vector2.Dot(Vector2.UnitX, direction);

            float distance = Vector2.Distance(position, colliderOrigin);
            if (distance > 20) return;

            float lerp = 1 - distance / 20;
            float rotation = -MathF.Sign(dot) * MathHelper.ToRadians(45);
            float offset = MathF.Sign(-dot) * 20;

            Rotation = MathHelper.Lerp(0, rotation, lerp);
            this.offset = Vector2.UnitX * MathHelper.Lerp(0, offset, lerp);
        }
    }
}
