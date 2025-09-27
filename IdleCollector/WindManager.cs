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
        private Random random;

        public WindManager()
        {
            Type = UpdateType.Slow;
            random = new Random();
            WindDirection = Vector2.UnitX;
            TotalWindMovement = Vector2.UnitX;
        }

        public Vector2 WindDirection { get; set; }
        public Vector2 TotalWindMovement { get; set; }
        public UpdateType Type { get; set; }

        public void Update(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            if (time - lastTimeStamp > waitTime)
            {
                targetDirection = new Vector2((float)random.NextDouble() - .5f, (float)random.NextDouble() - .5f);
                targetWindSpeed = (float)(random.NextDouble() + 1) * 5;

                if (targetDirection != Vector2.Zero)
                    targetDirection.Normalize();

                waitTime = (float)(random.NextDouble() * 20 + 1.0f);
                lastTimeStamp = time;
            }

            WindDirection = Vector2.Normalize(Vector2.LerpPrecise(WindDirection, targetDirection, lerpSpeed));
            windSpeed = MathHelper.Lerp(windSpeed, targetWindSpeed, lerpSpeed);
            TotalWindMovement += WindDirection * windSpeed;
        }
    }
}
