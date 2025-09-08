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
        public UpdateType Type { get; protected set; }

        public void Update(GameTime gameTime);
    }
}
