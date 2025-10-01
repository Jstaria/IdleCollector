using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IdleCollector
{
    internal class Player : Entity
    {
        public Player(Texture2D spriteSheet, Point position, Rectangle bounds, Point frameCount)
        {
            this.spriteSheet = spriteSheet;
            this.Position = position.ToVector2();
            this.Bounds = bounds;
            this.FrameCount = frameCount;
            this.Type = UpdateType.Controlled;
            this.CollisionType = CollisionType.Circle;
            this.Origin = new Vector2(bounds.Width / 2, bounds.Height * .65f);
            LoadPlayerData("PlayerData", "SaveData");
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
            GetInput();
            ClampPosition();
            OnPositionSpawnFlora(gameTime);
            InvokeOnMove(this);
        }

        public override void StandardUpdate(GameTime gameTime)
        { }
        
        public override void SlowUpdate(GameTime gameTime)
        { }

        public override void Draw(SpriteBatch sb)
        {
            sb.Draw(spriteSheet, new Rectangle(Position.ToPoint(), Bounds.Size), new Rectangle(64 * CurrentFrame.X, 64 * CurrentFrame.Y, 64, 64), Color.White, 0, Origin, SpriteEffects.None, LayerDepth);
        }

        private void OnPositionSpawnFlora(GameTime gameTime)
        {
            prevSpawnTime++;

            if (prevSpawnTime < spawnFrequency * 60) return;
            
            prevSpawnTime = 0;
            InvokeOnSpawn(this);
        }

        private void ClampPosition()
        {
            Vector2 position = Vector2.Zero;

            if (WorldBounds == Rectangle.Empty) return;

            position.X = Math.Clamp(Position.X, WorldBounds.Left + Bounds.Width / 6, WorldBounds.Right - Bounds.Width / 6);
            position.Y = Math.Clamp(Position.Y, WorldBounds.Top + Bounds.Height / 6, WorldBounds.Bottom - Bounds.Height / 6);

            Position = position;
        }

        private void GetInput()
        {
            Vector2 direction = Vector2.Zero;
            bool[] keyBools = new bool[4];

            if (keyBools[0] = Input.IsButtonDown(Keys.W)) direction.Y--;
            if (keyBools[1] = Input.IsButtonDown(Keys.A)) direction.X--;
            if (keyBools[2] = Input.IsButtonDown(Keys.S)) direction.Y++;
            if (keyBools[3] = Input.IsButtonDown(Keys.D)) direction.X++;

            SetSpriteDirection(keyBools);

            //if (direction != Vector2.Zero)
            //    direction.Normalize();
            Move(direction * speed);
        }

        private void SetSpriteDirection(bool[] bools)
        {
            if (bools[0])
            {
                SetFrame(CurrentFrame.X, 2);
                if (bools[1]) SetFrame(CurrentFrame.X, 4);
                if (bools[3]) SetFrame(CurrentFrame.X, 5);
            }
            else if (bools[2])
            {
                SetFrame(CurrentFrame.X, 3);
                if (bools[1]) SetFrame(CurrentFrame.X, 6);
                if (bools[3]) SetFrame(CurrentFrame.X, 7);
            }
            else if (bools[1])
            {
                SetFrame(CurrentFrame.X, 0);
                if (bools[0]) SetFrame(CurrentFrame.X, 4);
                if (bools[2]) SetFrame(CurrentFrame.X, 6);
            }
            else if (bools[3])
            {
                SetFrame(CurrentFrame.X, 1);
                if (bools[0]) SetFrame(CurrentFrame.X, 5);
                if (bools[2]) SetFrame(CurrentFrame.X, 7);
            }
        }

        private void LoadPlayerData(string name, string folder)
        {
            List<string> data = FileIO.ReadFrom(name, folder);
            FileIO.LoadDataInto(this, data);
        }
    }
}
