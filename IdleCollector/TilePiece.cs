using IdleEngine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class TilePiece: ICollidable
    {
        private string textureKey;
        private Rectangle bounds;

        public bool debugColorSwap {  get; set; }
        public string TextureKey { get => textureKey; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public bool IsCollidable { get; set; }
        public Point TilePosition { get; set; }

        public TilePiece(Rectangle bounds, string textureKey, Point tilePosition)
        {
            this.bounds = bounds;
            this.textureKey = textureKey;
            this.CollisionType = CollisionType.Rectangle;
            Position = bounds.Location.ToVector2();
            TilePosition = tilePosition;
        }
    }
}
