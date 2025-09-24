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
    }
}
