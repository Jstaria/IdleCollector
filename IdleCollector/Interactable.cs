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

        protected Interactable() { }

        public UpdateType Type { get; set; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
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

        public abstract void ControlledUpdate(GameTime gameTime);
        public abstract void StandardUpdate(GameTime gameTime);
        public abstract void SlowUpdate(GameTime gameTime);
        public virtual void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, textureSourceRect, Color, Rotation, Origin, SpriteEffects.None, LayerDepth);
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

        public abstract void InteractWith(Entity entity);
        public abstract void Nudge(float strength);
        public abstract void ApplyWind(Vector2 windScroll, FastNoiseLite noise);
    }
}
