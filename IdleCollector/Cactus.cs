using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class Cactus : Interactable
    {
        private Rectangle leftCactus;
        private Rectangle middleCactus;
        private Rectangle rightCactus;

        private bool MultiStructure;
        private bool[] flipSprite;

        float leftRotation;
        float rightRotation;

        private Vector2 origin;

        public override Vector2 Origin { get => origin; }
        private Vector2 LeftOrigin { get => new Vector2(leftCactus.Width * .975f, leftCactus.Height * .75f); }
        private Vector2 RightOrigin { get => new Vector2(rightCactus.Width * .025f, rightCactus.Height * .75f); }

        private Spring rotSpringLeft;
        private Spring rotSpringRight;

        public Cactus()
        {
            Random random = new Random();

            MultiStructure = random.Next(0, 2) == 1;
            rotationAmt = MathHelper.ToRadians(7.5f);
            xOffsetAmt = Vector2.UnitX;

            flipSprite = new bool[3];
            for (int i = 0; i < 3; i++)
                flipSprite[i] = random.Next(0, 2) == 1;

            posSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotSpringLeft = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotSpringRight = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);

            Stats = SpawnManager.Instance.GetStats("Cactus");

            switch (MultiStructure)
            {
                case true:
                    SetupMulti(random);
                    break;

                case false:
                    SetupSingle(random);
                    break;
            }
        }

        private void SetupSingle(Random random)
        {
            double randNum = random.NextDouble();
            Rectangle rect = Rectangle.Empty;

            if (randNum < Stats.RareSpawnChance)
            {
                rect = ResourceAtlas.GetRandomTileRect("rareCactus");
                if (rect.Height == 112)
                {
                    origin = new Vector2(rect.Width / 2, rect.Height * .625f);
                    rotationAmt = MathHelper.ToRadians(5f);
                }
            }
            else
            {
                rect = ResourceAtlas.GetRandomTileRect("cactus");
                origin = new Vector2(rect.Width / 2, rect.Height * .75f);
            }

            Bounds = new Rectangle(Bounds.Location, rect.Size);
            middleCactus = rect;
        }

        private void SetupMulti(Random random)
        {
            leftCactus = ResourceAtlas.GetRandomTileRect("cactusLeft");
            middleCactus = ResourceAtlas.GetRandomTileRect("cactusMiddle");
            rightCactus = ResourceAtlas.GetRandomTileRect("cactusRight");
        }

        public override void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            float noiseValue = noise.GetNoise(Position.X + windScroll.X, Position.Y + windScroll.Y);
            rotSpring.Nudge(noiseValue);

            if (!MultiStructure) return;

            int offset = 75;

            noiseValue = noise.GetNoise(Position.X - offset + windScroll.X, Position.Y + windScroll.Y);
            rotSpringLeft.Nudge(noiseValue);
            noiseValue = noise.GetNoise(Position.X + offset + windScroll.X, Position.Y + windScroll.Y);
            rotSpringRight.Nudge(noiseValue);
        }

        public override void InteractWith(ICollidable collider)
        {
            SetRotation(collider);
        }

        public override void Nudge(float strength)
        {
            posSpring.Nudge(strength);
            rotSpring.Nudge(MathHelper.ToRadians(strength));
        }

        public override void SetRotation(ICollidable collider)
        {
            Vector2 colliderOrigin = collider.Position;
            Vector2 position = Position;
            Vector2 direction = position - colliderOrigin;
            direction.Normalize();

            float dot = Vector2.Dot(Vector2.UnitX, direction);

            float amt = 20;

            float distance = Vector2.Distance(position, colliderOrigin);
            if (distance > amt) return;

            float lerp = 1 - distance / amt;
            xOffsetAmt = direction * amt * .2f; // Vector2.UnitX * MathF.Sign(dot) * amt * .75f;

            posSpring.RestPosition = lerp;
        }

        public override void Update(GameTime gameTime)
        {
            posSpring.Update();
            rotSpring.Update();
            Rotation = rotSpring.Position * rotationAmt;

            if (!MultiStructure) return;

            rotSpringLeft.Update();
            rotSpringRight.Update();
            leftRotation = rotSpringLeft.Position * rotationAmt;
            rightRotation = rotSpringRight.Position * rotationAmt;
        }
        public override void Draw(SpriteBatch sb)
        {
            Vector2 offset = xOffsetAmt * posSpring.Position;
            Rectangle rect = new Rectangle(Bounds.Location + offset.ToPoint(), Bounds.Size);

            float yPos = Position.Y + offset.Y + Origin.Y *.65f + Rotation;
            LayerDepth = WorldManager.GetLayerDepth(yPos);

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, middleCactus, DrawColor, Rotation, Origin, flipSprite[0] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, LayerDepth);

            if (!MultiStructure) return;
            sb.Draw(ResourceAtlas.TilemapAtlas, rect, !flipSprite[1] ? leftCactus : rightCactus, DrawColor, leftRotation, LeftOrigin, flipSprite[1] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, LayerDepth - .0001f);
            sb.Draw(ResourceAtlas.TilemapAtlas, rect, !flipSprite[2] ? rightCactus : leftCactus, DrawColor, rightRotation, RightOrigin, flipSprite[2] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, LayerDepth - .0001f);
        }
    }
}
