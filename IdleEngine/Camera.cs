using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public class Camera: IUpdatable
    {
        enum MovementType
        {
            Lerp,
            Spring
        }

        public Camera(int ViewWidth, int ViewHeight, float angularFrequency, float dampingRatio)
        {
            viewportSize = new Point(ViewWidth, ViewHeight);
            movementSprings = new[] { 
                new Spring(angularFrequency, dampingRatio, 0), 
                new Spring(angularFrequency, dampingRatio, 0) };
            movementType = MovementType.Spring;
        }
        public Camera(int ViewWidth, int ViewHeight, float lerpSpeed)
        {
            viewportSize = new Point(ViewWidth, ViewHeight);
            this.lerpSpeed = lerpSpeed;
            movementType = MovementType.Lerp;
        }

        private Point viewportSize;
        private Matrix transform;
        private Spring[] movementSprings;
        private Point targetPoint = Point.Zero;
        private float lerpSpeed;
        private MovementType movementType;
        private Vector2 actualPosition;

        public Point Position { get; set; }
        public Matrix Transform 
        { 
            get { return transform; }
            private set { transform = value; }
        }

        public UpdateType Type { get; set; }

        private void SetPosition(Point target) => SetPosition(target.X, target.Y);
        private void SetPosition(Vector2 target) => SetPosition((int)target.X, (int)target.Y);
        private void SetPosition(Vector3 target) => SetPosition((int)target.X, (int)target.Y);
        private void SetPosition(int x, int y)
        {
            Matrix position = Matrix.CreateTranslation(-x, -y, 0);
            Matrix offset = Matrix.CreateTranslation(viewportSize.X / 2, viewportSize.Y / 2, 0);

            Position = new Point(-x + viewportSize.X / 2, -y + viewportSize.Y / 2);

            transform = position * offset;
        }

        public void SetTarget(Rectangle target)
        {
            Point position = new Point(-target.X - (target.Width / 2), -target.Y - (target.Height / 2));
            SetTarget(position);
        }
        public void SetTranslation(Point position)
        {
            SetTarget(position);
            SetPosition(position);
            actualPosition = position.ToVector2();
        }

        public void SetTarget(Point target)
        {
            switch (movementType)
            {
                case MovementType.Lerp:
                    targetPoint = target;
                    break;

                case MovementType.Spring:
                    movementSprings[0].RestPosition = target.X;
                    movementSprings[1].RestPosition = target.Y;
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            switch (movementType)
            {
                case MovementType.Lerp:
                    actualPosition = Vector2.Lerp(actualPosition, targetPoint.ToVector2(), lerpSpeed);
                    SetPosition(actualPosition);
                    break;

                case MovementType.Spring:
                    foreach (Spring spring in movementSprings)
                        { spring.Update(); }
                    SetPosition((int)movementSprings[0].Position, (int)movementSprings[1].Position);
                    break;
            }
        }
    }
}
