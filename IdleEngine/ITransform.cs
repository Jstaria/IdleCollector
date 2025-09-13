using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public interface ITransform
    {
        public Vector2 Position { get; protected set; }
        
        public void Move(Vector2 direction);
        public void MoveTo(Point position);
    }
}
