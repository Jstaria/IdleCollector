using IdleEngine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class TestCollider : ICollidable
    {
        public CollisionType CollisionType { get; set; }
        public Point Position { get; set; }
        public int Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
    }
}
