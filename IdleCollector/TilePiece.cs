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
    internal class TilePiece: ICollidable, IRenderable, IUpdatable
    {
        #region // Variables
        private string textureKey;
        private string tileType;
        private Rectangle bounds;
        private Color color;
        private Dictionary<EmptyCollider, List<Interactable>> interactables;
        private float layerDepth;

        public bool debugColorSwap {  get; set; }
        public string TextureKey { get => textureKey; }
        public string TileType { get => tileType; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public int Radius { get; set; }
        public bool IsCollidable { get; set; }
        public Point TilePosition { get; set; }
        public Color Color { get => color; set => color = value; }
        public float LayerDepth { get => layerDepth; set => layerDepth = value; }
        public UpdateType Type { get; set; }
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

        public void SpawnGrass(ICollidable collider, Rectangle worldBounds)
        {
            foreach (KeyValuePair<EmptyCollider, List<Interactable>> pairs in interactables)
            {
                if (pairs.Value.Count > 0) continue;
                //if (!CollisionHelper.CheckForCollision(collider, pairs.Key)) continue;

                Random random = new Random();
                Rectangle keyBounds = pairs.Key.Bounds;

                for (int i = 0; i < 8; i++)
                {
                    Vector2 position = new Vector2(
                    keyBounds.Left + (float)random.NextDouble() * keyBounds.Width,
                    keyBounds.Top + (float)random.NextDouble() * keyBounds.Height);

                    Grass grass = new Grass("grass", ResourceAtlas.GetRandomAtlasKey("grass"));
                    grass.CollisionType = CollisionType.Circle;
                    grass.Radius = 8;
                    grass.Position = position;
                    grass.Bounds = new Rectangle(position.ToPoint(), new Point(16, 16));
                    grass.LayerDepth = (position.Y - worldBounds.Y - grass.Origin.Y) / (float)worldBounds.Height + float.Epsilon;
                    grass.Color = Color;

                    interactables[pairs.Key].Add(grass);
                }
            }
        }

        public void CheckInteractables(ICollidable collider)
        {
            foreach (List<Interactable> interactables in interactables.Values)
                foreach (Interactable interactable in interactables)
                {
                    interactable.InteractWith(collider);
                }
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, ResourceAtlas.GetTileRect(TileType, TextureKey), color, 0, Vector2.Zero, SpriteEffects.None, layerDepth);
        
            foreach (List<Interactable> interactables in  interactables.Values)
                foreach (Interactable interactable in interactables)
                    interactable.Draw(sb);
        }

        public void Update(GameTime gameTime)
        {
            foreach (List<Interactable> interactables in interactables.Values)
                foreach (Interactable interactable in interactables)
                    interactable.Update(gameTime);
        }
    }
}
