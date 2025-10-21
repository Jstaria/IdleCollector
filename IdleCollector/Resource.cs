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
    internal class Resource : Entity
    {
        private ResourceInfo info;
        private Rectangle textureRect;
        private Rectangle drawRect;
        private Point frameSize;
        private Spring2D posSpring;
        private bool isSpringActive;
        private GetVector followPos;

        public Resource(ResourceInfo info, string cactusFlower, int fps, Point frameCount, Vector2 position)
        {
            this.info = info;
            this.textureRect = ResourceAtlas.GetRandomTileRect(cactusFlower);
            this.frameSpeed = fps;
            this.frameSize = textureRect.Size / frameCount;
            this.FrameCount = frameCount;
            this.Position = position;
            this.drawRect = new Rectangle(textureRect.Location, frameSize);
            this.posSpring = new Spring2D(20, .25f, position);
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
            if (!isSpringActive) return;

            posSpring.RestPosition = followPos.Invoke();
            posSpring.Update();
        }

        public override void SlowUpdate(GameTime gameTime)
        {
            if (Vector2.DistanceSquared(posSpring.Position, followPos.Invoke()) > 400) return;


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
            throw new Exception("Resource not implemented");
        }

        public void ToggleSpring() => isSpringActive = !isSpringActive;
    }
}
