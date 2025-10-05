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
    public abstract class Entity: IAnimatable, IScene, ITransform, ICollidable
    {
        protected Texture2D spriteSheet;
        protected int speed;
        protected int spawnRange;
        protected int interactRange;
        protected float spawnFrequency;
        protected int prevSpawnTime;
        protected Vector2 velocity;

        public Point FrameCount { get; set; }
        public Point CurrentFrame { get; set; }
        public bool IsPlaying { get; set; }
        public UpdateType Type { get; set; }
        public Vector2 Position { get; set; }
        public CollisionType CollisionType { get; set; }
        public int Radius { get => spawnRange; set => spawnRange = value; }
        public int InteractRange { get => interactRange; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public Rectangle WorldBounds { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }
        public Vector2 Origin { get; set; }

        public delegate void EntityWalk(Entity entity);
        public event EntityWalk OnSpawn;
        public event EntityWalk OnMove;

        public void InvokeOnSpawn(Entity entity) => OnSpawn?.Invoke(entity);
        public void InvokeOnMove(Entity entity) => OnMove?.Invoke(entity);

        public abstract void ControlledUpdate(GameTime gameTime);
        public abstract void StandardUpdate(GameTime gameTime);
        public abstract void SlowUpdate(GameTime gameTime);
        public abstract void Draw(SpriteBatch sb);

        public virtual void Move(Vector2 direction) => Position += direction;
        public virtual void MoveTo(Point position) => Position = position.ToVector2();
        public virtual void NextFrame() => CurrentFrame = new Point((CurrentFrame.X + 1) % FrameCount.X, CurrentFrame.Y);
        public virtual void PrevFrame() => CurrentFrame = new Point((CurrentFrame.X - 1) % FrameCount.X, CurrentFrame.Y);
        public virtual void Pause() => IsPlaying = false;
        public virtual void Play() => IsPlaying = true;
        public virtual void SetFrame(int x, int y) => CurrentFrame = new Point(x, y);
    }
}
