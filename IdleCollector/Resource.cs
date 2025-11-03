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
        private Texture2D shadow;
        private ResourceInfo info;
        private Rectangle textureRect;
        private Rectangle drawRect;
        private Point frameSize;
        private Spring2D posSpring;
        private bool isSpringActive;
        private Vector2 offset;
        private float floatDistance = 3;
        private float floatSpeed = 2;
        private float randomOffsetAmt;
        private Vector2 velocity;
        private Curve<float> sizeCurve;
        private float size;
        private float aliveTime;
        private float spawnTime = 1f;

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
            this.velocity = Vector2.Normalize(RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One)) * RandomHelper.Instance.GetFloat(1,1.5f);
            this.shadow = ResourceAtlas.GetTexture("shadow");
            this.sizeCurve = (t) =>
            {
                float c4 = (2 * MathF.PI) / 3;

                return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
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

            LayerDepth = WorldManager.GetLayerDepth(Position.Y + drawRect.Height * 5.125f);
        }

        public override void Draw(SpriteBatch sb)
        {
            int size = (int)(32 * (((offset.Y + floatDistance) / MathF.Pow(floatDistance, 2.25f))));
            sb.Draw(ResourceAtlas.TilemapAtlas, Position + offset, drawRect, Color.White, 0, frameSize.ToVector2() / 2, this.size, SpriteEffects.None, LayerDepth);
            sb.Draw(shadow, new Rectangle(Position.ToPoint() + (Vector2.UnitY * floatDistance * 2).ToPoint(), new Point(size,size)), null, Color.White, 0, new Vector2(32, 32), SpriteEffects.None, LayerDepth);
        }

        public void OnPlayerWalk(Entity entity)
        {
            float distance = Vector2.DistanceSquared(entity.Position, Position);

            if (distance > entity.PickupRange * entity.PickupRange) return;

            isSpringActive = true;
            posSpring.RestPosition = entity.Position;

            if (distance > 400) return;

            ResourceManager.Instance.SpawnResourceUIObj(Position, info);

            Despawn?.Invoke(this);
        }

        public void ToggleSpring() => isSpringActive = !isSpringActive;
    }
}
