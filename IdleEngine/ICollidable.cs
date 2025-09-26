using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum CollisionType
{
    Circle,
    Rectangle,
    Both,
    // Polygon
}

namespace IdleEngine
{
    public interface ICollidable
    {
        public CollisionType CollisionType { get; protected set; }
        public Vector2 Position { get; protected set; }
        public int Radius { get; protected set; }
        public Rectangle Bounds { get; protected set; }
        public bool IsCollidable { get; protected set; }
        public Vector2 Origin { get; set; }
    }
}
