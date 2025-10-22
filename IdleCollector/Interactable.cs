using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public abstract class Interactable: IRenderable, IUpdatable, ICollidable
    {
        protected string textureKey;
        protected string tileType;
        protected Rectangle textureSourceRect;

        protected Vector2 xOffsetAmt;
        protected float rotationAmt;
        protected Spring posSpring;
        protected Spring rotSpring;
        protected List<Resource> spawnedResources;
        protected List<Resource> toRemove;
        protected ResourceInfo spawnedResourceInfo;
        protected float spawnAmt;
        protected float productionRate;
        protected float passedTime;

        protected Interactable() { }

        public UpdateType Type { get; set; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }
        public Color DrawColor { get; set; }
        public float Rotation { get; set; }
        public virtual Vector2 Origin { get; set; }
        public float WorldDepth { get; set; }
        public int WorldHeight { get; internal set; }
        public InteractableStats Stats { get; set; }

        public virtual void ControlledUpdate(GameTime gameTime)
        {
            if (spawnedResources == null) return;

            foreach (Resource resource in spawnedResources)
                resource.ControlledUpdate(gameTime);
        }
        public virtual void StandardUpdate(GameTime gameTime)
        {
            if (spawnedResources == null) return;

            foreach (Resource resource in spawnedResources)
                resource.StandardUpdate(gameTime);

            foreach (Resource resource in toRemove)
                spawnedResources.Remove(resource);

            toRemove.Clear();
        }
        public virtual void SlowUpdate(GameTime gameTime)
        {
            if (spawnedResources == null) return;

            foreach (Resource resource in spawnedResources)
                resource.SlowUpdate(gameTime);
        }
        public virtual void Draw(SpriteBatch sb)
        {
            if (spawnedResources == null) return;

            foreach (Resource resource in spawnedResources)
                resource.Draw(sb);
        }
        public virtual void SetRotation(Entity collider, float amt, float offsetModifier, bool rotate)
        {
            Vector2 direction = CollisionHelper.GetDirection(this, collider);

            float dot = Vector2.Dot(Vector2.UnitX, direction);

            float distance = CollisionHelper.GetDistance(this, collider);

            if (distance > amt) return;

            float lerp = 1 - distance / amt;
            xOffsetAmt = direction * amt * offsetModifier; // Vector2.UnitX * MathF.Sign(dot) * amt * .75f;

            posSpring.RestPosition = lerp;

            if (!rotate) return;

            rotationAmt = Math.Sign(dot) * MathHelper.ToRadians(45);
            rotSpring.RestPosition = lerp;
        }

        public virtual void InteractWith(Entity entity)
        {
            if (spawnedResources == null) return;
            
            foreach (Resource resource in spawnedResources)
                resource.OnPlayerWalk(entity);
        }
        public virtual void SecondaryInteractWith(Entity enity) { }
        public abstract void Nudge(float strength);
        public abstract void ApplyWind(Vector2 windScroll, FastNoiseLite noise);

        protected virtual void SpawnResource(string name, int fps, Point frameCount, ResourceInfo info, GameTime gameTime)
        {
            passedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (passedTime < 1) return;

            passedTime = 0;
            spawnAmt += productionRate;

            if (spawnAmt >= 1)
            {
                spawnAmt--;

                Resource resource = new Resource(info, name, fps, frameCount, Position);
                resource.Despawn = Despawn;
                spawnedResources.Add(resource);
            }
        }
        protected virtual void Despawn(Resource r) => toRemove.Add(r);
    }
}
