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
    public class CollisionTree<T> : ICollidable, IRenderable
    {
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }
        public Vector2 Origin { get; set; }

        private TreeLeaf<T> root;

        private List<TreeLeaf<T>> activeLeaves;

        public CollisionTree(Rectangle bounds, int depth)
        {
            Bounds = bounds;
            Position = bounds.Location.ToVector2();
            activeLeaves = new();

            root = new TreeLeaf<T>(Bounds, 0, depth);
        }

        public void GetActiveLeaves(ICollidable collider, CollisionCheck type)
        {
            activeLeaves = root.GetContainingLeaves(collider, type);
        }

        public List<T> GetCollidedWith(ICollidable collider, CollisionCheck type)
        {
            List<T> containingChildren = new List<T>();
            foreach (TreeLeaf<T> child in activeLeaves)
                containingChildren.AddRange(child.containingChildren);

            return containingChildren;
        }

        public void AddChild(T child, Point position)
        {
            root.AddChild(child, position);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), 2, Color.Black);
            sb.DrawLine(Bounds.Location.ToVector2(), (Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), 2, Color.Black);
            sb.DrawLine((Bounds.Location + new Point(Bounds.Width, 0)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), 2, Color.Black);
            sb.DrawLine((Bounds.Location + new Point(0, Bounds.Height)).ToVector2(), (Bounds.Location + new Point(Bounds.Width, Bounds.Height)).ToVector2(), 2, Color.Black);

            root.Draw(sb);
        }

        public void DrawActiveBounds(SpriteBatch sb)
        {
            foreach (TreeLeaf<T> leaf in activeLeaves)
                leaf.DrawBounds(sb);
        }
    }
}
