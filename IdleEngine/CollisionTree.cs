using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public class CollisionTree : ICollidable, IRenderable
    {
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }

        private TreeLeaf[,] children;

        public CollisionTree(Rectangle bounds, int depth)
        {
            Bounds = bounds;
            Position = bounds.Location.ToVector2();

            children = new TreeLeaf[2, 2];

            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    children[i, j] = new TreeLeaf(Bounds, 0, depth);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), 2, Color.Black);
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), 2, Color.Black);
            sb.DrawLine((Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), 2, Color.Black);
            sb.DrawLine((Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), 2, Color.Black);

            foreach (TreeLeaf leaf in children)
            {
                leaf.Draw(sb);
            }
        }
    }
}
