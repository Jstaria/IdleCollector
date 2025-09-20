using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IdleEngine
{
    internal class TreeLeaf<T> : ICollidable, IRenderable
    {
        internal List<T> containingChildren;

        internal bool IsLeaf { get; set; }
        internal TreeLeaf<T>[,] Children { get; set; }
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
            containingChildren = new();
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
            Children = new TreeLeaf<T>[2, 2];
            IsLeaf = false;

            int childWidth = Bounds.Width / 2;
            int childHeight = Bounds.Height / 2;

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 2; i++)
                {
                    Rectangle bounds = new Rectangle(Bounds.Location + new Point(childWidth * i, childHeight * j), new Point(childWidth, childHeight));
                    Children[i, j] = (new TreeLeaf<T>(bounds, Depth + 1, maxDepth));
                }
            }
        }

        public void AddChild(T containingChild, Point position) 
        { 
            if (Bounds.Contains(position)) 
            { 
                if (!IsLeaf) 
                { 
                    foreach (TreeLeaf<T> leaf in Children) 
                    { 
                        if (leaf.Bounds.Contains(position)) 
                        { 
                            leaf.AddChild(containingChild, position); 
                        } 
                    } 
                }
                else
                {
                    containingChildren.Add(containingChild);
                    return;
                } 
            } 
        }

        public List<TreeLeaf<T>> GetContainingLeaves(ICollidable collider, CollisionCheck type)
        {
            List<TreeLeaf<T>> leaves = new();

            if (CollisionHelper.CheckForCollision(collider, this, type))
            {
                if (IsLeaf)
                    leaves.Add(this);

                if (!IsLeaf)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        TreeLeaf<T> node = Children[i % 2, i / 2];

                        if (CollisionHelper.CheckForCollision(collider, node, type))
                        {
                            leaves.AddRange(node.GetContainingLeaves(collider, type));
                        }
                    }
                }
            }

            return leaves;
        }

        public void Draw(SpriteBatch sb)
        {
            float size = (maxDepth - Depth) * 5;
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine((Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);
            sb.DrawLine((Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), size, colors[Depth % colors.Length]);

            if (Children == null) return;

            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    Children[x, y]?.Draw(sb);
        }

        public void DrawBounds(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.GetTexture("square"), Bounds, colors[Depth] * .4f);
        }
    }
}
