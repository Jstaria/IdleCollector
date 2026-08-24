using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public abstract class PostProcess
    {
        public PostProcess() { }

        public abstract void Draw(SpriteBatch sb, ref RenderTarget2D renderTarget);
    }
}
