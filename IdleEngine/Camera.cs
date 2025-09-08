using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadow
{
    internal class Camera
    {
        public Camera(int ViewWidth, int ViewHeight)
        {
            viewportSize = new Point(ViewWidth, ViewHeight);
        }

        private Point viewportSize;
        private Matrix transform;

        public Matrix Transform 
        { 
            get { return transform; }
            private set { transform = value; }
        }

        public void Follow(Rectangle target)
        {
            Point position = new Point(-target.X - (target.Width / 2), -target.Y - (target.Height / 2));
            
            Follow(position);
        }

        public void Follow(Point target)
        {
            Matrix position = Matrix.CreateTranslation(-target.X, -target.Y, 0);

            Matrix offset = Matrix.CreateTranslation(viewportSize.X / 2, viewportSize.Y / 2, 0);

            transform = position * offset;
        }

        public void FollowDirection(Vector2 direction, Matrix transfrom)
        {
            Matrix directionM = Matrix.CreateTranslation(
                -direction.X,
                -direction.Y,
                0);

            Matrix offset = Matrix.CreateTranslation(
                viewportSize.X / 2,
                viewportSize.Y / 2,
                0);

            Transform = transfrom * directionM * offset;
        }

    }
}
