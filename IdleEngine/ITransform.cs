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
        public Point Position { get; protected set; }
        
        public void Move(Point direction);
        public void MoveTo(Point position);
    }
}
