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

        protected Interactable() { }

        public UpdateType Type { get; set; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }
        public float Rotation { get; set; }
        public virtual Vector2 Origin { get; set; }
        public float WorldDepth { get; set; }
        public int WorldHeight { get; internal set; }

        public abstract void Update(GameTime gameTime);
        public virtual void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, textureSourceRect, Color, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }
        public abstract void SetRotation(ICollidable collider);
        public abstract void InteractWith(ICollidable collider);
    }
}
