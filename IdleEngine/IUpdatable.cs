using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public interface IUpdatable
    {
        public void ControlledUpdate(GameTime gameTime);
        public void StandardUpdate(GameTime gameTime);
        public void SlowUpdate(GameTime gameTime);
    }
}
