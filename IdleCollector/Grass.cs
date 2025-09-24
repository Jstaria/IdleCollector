using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class Grass : Interactable
    {
        public Grass(string atlasType, string atlasKey)
        {
            tileType = atlasType;
            textureKey = atlasKey;
        }

        public override void Update(GameTime gameTime)
        {

        }

        public override void InteractWith(ICollidable collider)
        {
            
        }

        public override void SetRotation(ICollidable collider, int tileOriginX)
        {
            Vector2 colliderOrigin = collider.Position - (collider.Bounds.Size.ToVector2() / 2);
            Vector2 position = Position + Origin;

            float distance = Vector2.Distance(position, colliderOrigin);
            if (distance > 20) return;

            float lerp = 1 - distance / 20;
            float rotation = MathF.Sign(position.X - tileOriginX) * 20;
            

            Rotation = MathHelper.Lerp(0, rotation, lerp);
        }
    }
}
