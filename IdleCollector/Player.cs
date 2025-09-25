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
    internal class Player : IAnimatable, IScene, ITransform, ICollidable
    {
        private Texture2D spriteSheet;
        private int speed;
        private int pickupRange;
        private float spawnFrequency;
        private float prevSpawnTime;

        public Point FrameCount { get; set; }
        public Point CurrentFrame { get; set; }
        public bool IsPlaying { get; set; }
        public UpdateType Type { get; set; }
        public Vector2 Position { get; set; }
        public CollisionType CollisionType { get; set; }
        public int Radius { get => pickupRange; set => pickupRange = value; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public Rectangle WorldBounds { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public delegate void PlayerWalk(ICollidable spawn);
        public event PlayerWalk OnSpawn;
        public event PlayerWalk OnMove;

        public Player(Texture2D spriteSheet, Point position, Rectangle bounds, Point frameCount)
        {
            this.spriteSheet = spriteSheet;
            this.Position = position.ToVector2();
            this.Bounds = bounds;
            this.FrameCount = frameCount;
            this.Type = UpdateType.Controlled;
            this.CollisionType = CollisionType.Circle;

            LoadPlayerData("PlayerData", "SaveData");
        }

        public void Update(GameTime gameTime)
        {
            GetInput();
            ClampPosition();
            OnPositionSpawnFlora(gameTime);
            OnMove?.Invoke(this);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(spriteSheet, new Rectangle(Position.ToPoint() - (Bounds.Size.ToVector2() / 2).ToPoint(), Bounds.Size), new Rectangle(64 * CurrentFrame.X, 64 * CurrentFrame.Y, 64, 64), Color.White, 0, Vector2.Zero, SpriteEffects.None, LayerDepth);
        }

        private void OnPositionSpawnFlora(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            if (time - prevSpawnTime < spawnFrequency) return;
            
            prevSpawnTime = time;
            OnSpawn?.Invoke(this);
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

        public void Move(Vector2 direction) => Position += direction;
        public void MoveTo(Point position) => Position = position.ToVector2();
        public void NextFrame() => CurrentFrame = new Point((CurrentFrame.X + 1) % FrameCount.X, CurrentFrame.Y);
        public void PrevFrame() => CurrentFrame = new Point((CurrentFrame.X - 1) % FrameCount.X, CurrentFrame.Y);
        public void Pause() => IsPlaying = false;
        public void Play() => IsPlaying = true;
        public void SetFrame(int x, int y) => CurrentFrame = new Point(x, y);
    }
}
