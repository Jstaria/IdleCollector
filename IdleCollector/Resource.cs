using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdleCollector
{
    public class Resource : Entity
    {
        private ResourceInfo info;
        private Rectangle textureRect;
        private Rectangle drawRect;
        private Point frameSize;
        private Spring2D posSpring;
        private bool isSpringActive;
        private GetVector followPos;
        private Vector2 offset;
        private float floatDistance = 2;
        private float floatSpeed = 2;
        private float randomOffsetAmt;
        private Vector2 velocity;
        private Curve<float> sizeCurve;
        private float size;
        private float aliveTime;
        private float spawnTime = 0.5f;

        public delegate void OnDespawn(Resource r);
        public OnDespawn Despawn;

        public Resource(ResourceInfo info, string cactusFlower, int fps, Point frameCount, Vector2 position)
        {
            this.info = info;
            this.textureRect = ResourceAtlas.GetRandomTileRect(cactusFlower);
            this.frameSpeed = fps;
            this.frameSize = textureRect.Size / frameCount;
            this.FrameCount = frameCount;
            this.Position = position;
            this.drawRect = new Rectangle(textureRect.Location, frameSize);
            this.posSpring = new Spring2D(15, 1f, position);
            this.randomOffsetAmt = RandomHelper.Instance.GetFloat(0, 10);
            this.velocity = RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One);

            this.sizeCurve = (t) =>
            {
                if (t < .4f)
                {
                    return -24 * MathF.Pow((t - .25f), 2) + 1.5f;
                }
                else if (t < .5f)
                {
                    return .65f * MathF.Cos(10 * (t + 7.395f)) + 1.5f;
                }
                else
                {
                    return -MathF.Pow(MathF.E, -25*(t - .4064f)) + 1;
                }
            };
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
            aliveTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float t = aliveTime / spawnTime;
            size = sizeCurve(t);
            if (!isSpringActive)
            {
                offset = Vector2.UnitY * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * floatSpeed + randomOffsetAmt) * floatDistance;
                Position += (velocity *= .95f);
            }
            else
            {
                posSpring.Update();
                Position = posSpring.Position;
            }
        }

        public override void SlowUpdate(GameTime gameTime)
        {

        }

        public override void StandardUpdate(GameTime gameTime)
        {
            Vector2 betweenFrame = this.InBetweenFrame;
            betweenFrame.X += FrameSpeed;
            InBetweenFrame = betweenFrame;

            float CurrentFrameX = 1 + ((betweenFrame.X % (FrameCount.X - 1)));
            SetFrame((int)CurrentFrameX, 0);

            drawRect.Location = CurrentFrame * frameSize + textureRect.Location;
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Position + offset, drawRect, Color.White, 0, frameSize.ToVector2() / 2, size, SpriteEffects.None, .25f);
        }

        public void OnPlayerWalk(Entity entity)
        {
            float distance = Vector2.DistanceSquared(entity.Position, Position);

            if (distance > entity.PickupRange * entity.PickupRange) return;

            isSpringActive = true;
            posSpring.RestPosition = entity.Position;

            if (distance > 400) return;

            ResourceManager.Instance.SpawnResourceUIObj(entity.Position, info);

            Despawn?.Invoke(this);
        }

        public void ToggleSpring() => isSpringActive = !isSpringActive;
    }
}
