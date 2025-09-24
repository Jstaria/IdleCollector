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

        protected Interactable() { }

        public UpdateType Type { get; set; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsCollidable { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public abstract void Update(GameTime gameTime);
        public virtual void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, ResourceAtlas.GetTileRect(tileType, textureKey), Color, 0, Vector2.Zero, SpriteEffects.None, LayerDepth);
        }
        public abstract void InteractWith(ICollidable collider);
    }
}
