using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public interface IRenderable
    {
        // Possibly add property here to control draw layer/order

        public void Draw(SpriteBatch sb);
    }
}
