using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public interface IDrawable
    {
        public Color TintColor { get; protected set; }
        public Point Size { get; protected set; }
        public Point DrawSize { get; protected set; }
        public Texture2D Texture { get; protected set; }

        // Possibly add property here to control draw layer/order

        public void Draw(SpriteBatch sb);
    }
}
