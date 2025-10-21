using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public class Spring2D
    {
        private Spring xPos;
        private Spring yPos;

        public Vector2 Position
        {
            get => new Vector2(xPos.Position, yPos.Position);
            set
            {
                xPos.Position = value.X;
                yPos.Position = value.Y;
            }
        }

        public Vector2 RestPosition
        {
            get => new Vector2(xPos.RestPosition, yPos.RestPosition);
            set
            {
                xPos.RestPosition = value.X;
                yPos.RestPosition = value.Y;
            }
        }


        public Spring2D(float angularFrequency, float dampingRatio, Vector2 restPosition)
        {
            xPos = new Spring(angularFrequency, dampingRatio, restPosition.X);
            yPos = new Spring(angularFrequency, dampingRatio, restPosition.Y);
        }

        public void Update()
        {
            xPos.Update();
            yPos.Update();
        }
        public void SetValues(float angularFrequency, float dampingRatio)
        {
            xPos.SetValues(angularFrequency, dampingRatio);
            yPos.SetValues(angularFrequency, dampingRatio);
        }

        public void Nudge(Vector2 velocity)
        {
            xPos.Nudge(velocity.X);
            yPos.Nudge(velocity.Y);
        }
    }
}
