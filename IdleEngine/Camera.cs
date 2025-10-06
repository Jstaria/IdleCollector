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

        private Point viewportSize;
        private Matrix transform;
        private Spring[] movementSprings;
        private Spring[] shakeSprings;
        private Point targetPoint = Point.Zero;
        private float lerpSpeed;
        private MovementType movementType;
        private Vector2 actualPosition;

        public Rectangle Bounds { get; private set; }
        public bool UseBounds {  get; set; }
        public Point Position { get; set; }
        public float Zoom { get; set; }
        public Matrix Transform 
        { 
            get { return transform; }
            private set { transform = value; }
        }

        public UpdateType Type { get; set; }

        public Camera(int ViewWidth, int ViewHeight, float angularFrequency, float dampingRatio)
        {
            viewportSize = new Point(ViewWidth, ViewHeight);
            movementSprings = new[] {
                new Spring(angularFrequency, dampingRatio, 0),
                new Spring(angularFrequency, dampingRatio, 0) };
            shakeSprings = new[] {
                new Spring(angularFrequency, dampingRatio, 0),
                new Spring(angularFrequency, dampingRatio, 0) };
            movementType = MovementType.Spring;
            Zoom = 1;
        }
        public Camera(int ViewWidth, int ViewHeight, float lerpSpeed)
        {
            viewportSize = new Point(ViewWidth, ViewHeight);
            this.lerpSpeed = lerpSpeed;
            movementType = MovementType.Lerp;
            Zoom = 1;
        }

        public void SetBounds(Rectangle bounds) => Bounds = bounds; 
        private void SetPosition(Point target) => SetPosition(target.X, target.Y);
        private void SetPosition(Vector2 target) => SetPosition((int)target.X, (int)target.Y);
        private void SetPosition(Vector3 target) => SetPosition((int)target.X, (int)target.Y);
        private void SetPosition(int x, int y)
        {
            Matrix position = Matrix.CreateTranslation(-x, -y, 0);
            Matrix offset = Matrix.CreateTranslation(viewportSize.X / 2 / Zoom - shakeSprings[0].Position, viewportSize.Y / 2 / Zoom - shakeSprings[1].Position, 0);
            Matrix zoom = Matrix.CreateScale(Zoom);

            Position = new Point(-x + viewportSize.X / 2, -y + viewportSize.Y / 2);

            transform = position * offset * zoom;
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

            if (movementType == MovementType.Spring)
            {
                movementSprings[0].Position = position.X;
                movementSprings[0].Velocity = 0;
                movementSprings[1].Position = position.Y;
                movementSprings[1].Velocity = 0;
            }
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

        public void ControlledUpdate(GameTime gameTime)
        {
            Vector2 position = Vector2.Zero;

            foreach (Spring spring in shakeSprings)
                spring.Update();

            switch (movementType)
            {
                case MovementType.Lerp:
                    actualPosition = Vector2.Lerp(actualPosition, targetPoint.ToVector2(), lerpSpeed);
                    position = actualPosition;
                    break;

                case MovementType.Spring:
                    foreach (Spring spring in movementSprings)
                        { spring.Update(); }
                    position = new Vector2(movementSprings[0].Position, movementSprings[1].Position);
                    break;
            }

            if (UseBounds)
            {
                if (Bounds.Width < viewportSize.X)
                {
                    position.X = (Bounds.Left) + (Bounds.Width / 2);
                }
                else position.X = Math.Clamp(position.X, Bounds.Left + viewportSize.X / 2, Bounds.Right - viewportSize.X / 2);

                if (Bounds.Height < viewportSize.Y)
                {
                    position.Y = (Bounds.Top) + (Bounds.Height / 2);
                }
                else position.Y = Math.Clamp(position.Y, Bounds.Top + viewportSize.Y / 2, Bounds.Bottom - viewportSize.Y / 2);
            }

            SetPosition(position);
        }

        void IUpdatable.StandardUpdate(GameTime gameTime)
        {

        }

        void IUpdatable.SlowUpdate(GameTime gameTime)
        {

        }
        
        public void ShakeCamera(float angularFrequency, float dampingRatio, Vector2 nudge)
        {
            foreach (Spring spring in shakeSprings)
                spring.SetValues(angularFrequency, dampingRatio);

            shakeSprings[0].Nudge(nudge.X);
            shakeSprings[1].Nudge(nudge.Y);
        }
    }
}
