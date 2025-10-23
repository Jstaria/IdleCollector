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
            this.posSpring = new Spring2D(20, 2f, position);
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
            if (!isSpringActive) return;

            posSpring.Update();
            Position = posSpring.Position;
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
            sb.Draw(ResourceAtlas.TilemapAtlas, Position, drawRect, Color.White, 0, frameSize.ToVector2() / 2, 1, SpriteEffects.None, .25f);
        }

        public void OnPlayerWalk(Entity entity)
        {
            float distance = Vector2.DistanceSquared(entity.Position, Position);

            if (distance > entity.PickupRange * entity.PickupRange) return;

            isSpringActive = true;
            posSpring.RestPosition = entity.Position;

            if (distance > 100) return;

            ResourceManager.Instance.SpawnResourceUIObj(entity.Position, info);

            Despawn?.Invoke(this);
        }

        public void ToggleSpring() => isSpringActive = !isSpringActive;
    }
}
