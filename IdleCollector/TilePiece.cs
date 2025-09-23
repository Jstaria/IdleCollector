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
    internal class TilePiece: ICollidable, IRenderable
    {
        #region // Variables
        private string textureKey;
        private string tileType;
        private Rectangle bounds;
        private Color color;
        private Dictionary<EmptyCollider, List<Interactable>> interactables;

        public bool debugColorSwap {  get; set; }
        public string TextureKey { get => textureKey; }
        public string TileType { get => tileType; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public bool IsCollidable { get; set; }
        public Point TilePosition { get; set; }
        public Color Color { get => color; }
        #endregion

        public TilePiece(Rectangle bounds, string textureKey, string tileType, Point tilePosition, Color color)
        {
            this.bounds = bounds;
            this.textureKey = textureKey;
            this.tileType = tileType;
            this.CollisionType = CollisionType.Rectangle;
            Position = bounds.Location.ToVector2();
            TilePosition = tilePosition;
            this.color = color;

            SetupInnerBounds();
        }

        private void SetupInnerBounds()
        {
            interactables = new();
            int childWidth = Bounds.Width / 2;
            int childHeight = Bounds.Height / 2;

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 2; i++)
                {
                    EmptyCollider tempCollider = new EmptyCollider();
                    Rectangle bounds = new Rectangle(Bounds.Location + new Point(childWidth * i, childHeight * j), new Point(childWidth, childHeight));
                    
                    tempCollider.Bounds = bounds;
                    tempCollider.Position = bounds.Location.ToVector2();
                    tempCollider.CollisionType = CollisionType.Rectangle;

                    interactables.Add(tempCollider, new());
                }
            }
        }

        public void SpawnGrass(ICollidable collider)
        {
            foreach (KeyValuePair<EmptyCollider, List<Interactable>> pairs in interactables)
            {
                if (pairs.Value.Count > 0) continue;
                if (!CollisionHelper.CheckForCollision(collider, pairs.Key)) continue;

                interactables[pairs.Key].Add(Interactable);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, ResourceAtlas.GetTileRect(TileType, TextureKey), color);
        }
    }
}
