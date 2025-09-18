using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    internal class TreeLeaf : ICollidable, IRenderable
    {
        internal bool IsLeaf { get; set; }
        internal TreeLeaf[,] Children { get; set; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public int Depth { get; set; }

        private int maxDepth;
        private Color[] colors;

        public TreeLeaf(Rectangle bounds, int depth, int maxDepth) 
        {
            colors = new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.YellowGreen, Color.Green, Color.LightBlue, Color.Blue, Color.BlueViolet, Color.Violet, Color.Purple };
            IsLeaf = true;

            Bounds = bounds;
            this.maxDepth = maxDepth;
            Depth = depth;
            Position = bounds.Location.ToVector2();

            if (depth == maxDepth) return;
            CreateChildren(maxDepth);
        }

        public void CreateChildren(int maxDepth)
        {
            Children = new TreeLeaf[2,2];
            IsLeaf = false;

            int childWidth = Bounds.Width / 2;
            int childHeight = Bounds.Height / 2;

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 2; i++)
                {
                    Rectangle bounds = new Rectangle(Bounds.Location + new Point(childWidth * i, childHeight * j), new Point(childWidth,childHeight));
                    Children[i,j] = (new TreeLeaf(bounds, Depth + 1, maxDepth));
                }
            }
        }

        public TreeLeaf GetContainingLeaf(ICollidable collider, CollisionCheck type)
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // TODO: ADD YOUR CODE FOR ACTIVITY TWO HERE
            // If the rectangle param fits inside me and I have children...
            // Check each of my children and see if it fits in them
            // And if so, then call this again
            // If I don't have children or none of my children has the rectangle, it must be me
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            if (CollisionHelper.CheckForCollision(collider, this, type))
            {  if (!IsLeaf)
                {  foreach (TreeLeaf node in Children)
                    { if (CollisionHelper.CheckForCollision(collider, node, type))
                        { return node.GetContainingLeaf(collider, type); 
                        } 
                    } 
                } return this;
            } return null;
        }

        public void Draw(SpriteBatch sb)
        {
            float size = (maxDepth - Depth) * 5;
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine((Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine((Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);

            if (IsLeaf) return;
            foreach (TreeLeaf leaf in Children)
            { leaf.Draw(sb); }
        }
    }
}
