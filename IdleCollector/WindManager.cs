using IdleEngine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class WindManager: IUpdatable
    {
        private float waitTime = 0;
        private float lastTimeStamp = 0;
        private float lerpSpeed = .01f;
        private float windSpeed = 1;

        private Vector2 targetDirection;
        private float targetWindSpeed;

        public WindManager()
        {
            Type = UpdateType.Slow;
            WindDirection = Vector2.UnitX;
            TotalWindMovement = Vector2.UnitX;
        }

        public Vector2 WindDirection { get; set; }
        public Vector2 TotalWindMovement { get; set; }
        public UpdateType Type { get; set; }

        public void SlowUpdate(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            RandomHelper random = RandomHelper.Instance;

            if (time - lastTimeStamp > waitTime)
            {
                targetDirection = random.GetVector2(-Vector2.One, Vector2.One);
                targetWindSpeed = random.GetFloat(1, 5);

                if (targetDirection != Vector2.Zero)
                    targetDirection.Normalize();

                waitTime = random.GetFloat(1,15);
                lastTimeStamp = time;
            }

            WindDirection = Vector2.Normalize(Vector2.LerpPrecise(WindDirection, targetDirection, lerpSpeed));
            windSpeed = MathHelper.Lerp(windSpeed, targetWindSpeed, lerpSpeed);
            TotalWindMovement += WindDirection * windSpeed;
        }

        void IUpdatable.ControlledUpdate(GameTime gameTime)
        {

        }

        void IUpdatable.StandardUpdate(GameTime gameTime)
        {

        }
    }
}
