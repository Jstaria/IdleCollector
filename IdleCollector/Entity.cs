using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
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
        
        [JsonProperty] protected int speed;
        [JsonProperty] protected int spawnRange;
        [JsonProperty] protected int interactRange;
        [JsonProperty] protected int pickupRange;
        [JsonProperty] protected float spawnFrequency;
        protected int prevSpawnTime;
        protected Vector2 velocity;
        protected float frameSpeed;

        [JsonIgnore] public Point FrameCount { get; set; }
        [JsonIgnore] public Point CurrentFrame { get; set; }
        [JsonIgnore] public Vector2 InBetweenFrame { get; set; }
        [JsonIgnore] public bool IsPlaying { get; set; }
        [JsonIgnore] public UpdateType Type { get; set; }
        [JsonIgnore] public Vector2 Position { get; set; }
        [JsonIgnore] public CollisionType CollisionType { get; set; }
        [JsonIgnore] public float Radius { get => spawnRange; set => spawnRange = (int)value; }
        [JsonIgnore] public int InteractRange { get => interactRange; }
        [JsonIgnore] public int PickupRange { get => pickupRange; }
        [JsonIgnore] public Rectangle Bounds { get; set; }
        [JsonIgnore] public bool IsCollidable { get; set; }
        [JsonIgnore] public Rectangle WorldBounds { get; set; }
        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public Color Color { get; set; }
        [JsonIgnore] public Vector2 Origin { get; set; }
        [JsonIgnore] public float FrameSpeed { get => frameSpeed / 60.0f; set => frameSpeed = value; }

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
        public virtual void MoveTo(Vector2 position) => Position = position;
        public virtual void NextFrame() => CurrentFrame = new Point((CurrentFrame.X + 1) % FrameCount.X, CurrentFrame.Y);
        public virtual void PrevFrame() => CurrentFrame = new Point((CurrentFrame.X - 1) % FrameCount.X, CurrentFrame.Y);
        public virtual void Pause() => IsPlaying = false;
        public virtual void Play() => IsPlaying = true;
        public virtual void SetFrame(int x, int y) => CurrentFrame = new Point(x, y);
    }
}
