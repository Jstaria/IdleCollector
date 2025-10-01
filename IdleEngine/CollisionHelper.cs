using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public enum CollisionCheck
    {
        Circle,
        Rectangle,
        CircleRect
    }
    public static class CollisionHelper
    {
        public static bool CheckForCollision(ICollidable c1, ICollidable c2)
        {
            if (c1.CollisionType == CollisionType.Both || c2.CollisionType == CollisionType.Both)
                throw new Exception("Cannot check for collision, please specify collision type!");

            if (c1.CollisionType == CollisionType.Circle && c2.CollisionType == CollisionType.Circle)
                return CircleCollision(c1, c2);
            else if ((c1.CollisionType == CollisionType.Circle && c2.CollisionType == CollisionType.Rectangle) ||
                c1.CollisionType == CollisionType.Rectangle && c2.CollisionType == CollisionType.Circle)
                return CircleRectangleCollision(c1, c2);
            else if (c1.CollisionType == CollisionType.Rectangle && c2.CollisionType == CollisionType.Rectangle)
                return RectangleCollision(c1, c2);
            else return false;
        }
        public static bool CheckForCollision(ICollidable c1, ICollidable c2, CollisionCheck collisionType)
        {
            switch (collisionType)
            {
                case CollisionCheck.Circle:
                    return CircleCollision(c1, c2);
                case CollisionCheck.Rectangle:
                    return RectangleCollision(c1, c2);
                case CollisionCheck.CircleRect:
                    return CircleRectangleCollision(c1, c2);
            }

            return false;
        }

        private static bool RectangleCollision(ICollidable c1, ICollidable c2)
        {
            return (c1.Position.X < c2.Position.X + c2.Bounds.Width &&
                    c1.Position.X + c1.Bounds.Width > c2.Position.X &&
                    c1.Position.Y < c2.Position.Y + c2.Bounds.Height &&
                    c1.Position.Y + c1.Bounds.Height > c2.Position.Y);
        }

        private static bool CircleRectangleCollision(ICollidable c, ICollidable r)
        {
            float testX = c.Position.X; float testY = c.Position.Y;

            if (c.Position.X < r.Position.X)
                testX = r.Position.X;
            else if (c.Position.X > r.Position.X + r.Bounds.Width)
                testX = r.Position.X + r.Bounds.Width;
            if (c.Position.Y < r.Position.Y)
                testY = r.Position.Y;
            else if (c.Position.Y > r.Position.Y + r.Bounds.Height)
                testY = r.Position.Y + r.Bounds.Height;

            float distX = c.Position.X - testX;
            float distY = c.Position.Y - testY;
            float dist = distX * distX + distY * distY;

            return dist <= c.Radius * c.Radius;
        }

        private static bool CircleCollision(ICollidable c1, ICollidable c2)
        {
            Vector2 pos1 = new Vector2(c1.Position.X, c1.Position.Y);
            Vector2 pos2 = new Vector2(c2.Position.X, c2.Position.Y);

            int radius = (int)MathF.Pow(c1.Radius + c2.Radius, 2);

            return Vector2.DistanceSquared(pos1, pos2) < radius;
        }

        public static float GetDistance(ICollidable c1, ICollidable c2)
        {
            Vector2 colliderOrigin = c1.Position;
            Vector2 position = c2.Position;

            float distance = Vector2.Distance(position, colliderOrigin);

            return distance;
        }

        public static Vector2 GetDirection(ICollidable c1, ICollidable c2)
        {
            Vector2 direction = c1.Position - c2.Position;
            if (direction != Vector2.Zero)
                direction.Normalize();

            return direction;
        }
    }
}
