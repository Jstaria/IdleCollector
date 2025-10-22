using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class TilePiece : ICollidable, IRenderable, IUpdatable
    {
        #region // Variables
        private string textureKey;
        private string tileType;
        private Rectangle bounds;
        private Color color;
        private List<InnerTile> innerTiles;
        private List<Interactable> producingInteractables;
        private float layerDepth;

        public bool debugColorSwap { get; set; }
        public string TextureKey { get => textureKey; }
        public string TileType { get => tileType; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public CollisionType CollisionType { get; set; }
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public bool IsCollidable { get; set; }
        public Point TilePosition { get; set; }
        public Color Color { get => color; set => color = value; }
        public float LayerDepth { get => layerDepth; set => layerDepth = value; }
        public UpdateType Type { get; set; }
        public Vector2 Origin { get; set; }
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
            innerTiles = new();
            producingInteractables = new();
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
                    tempCollider.Radius = 0;

                    innerTiles.Add(new InnerTile(tempCollider));
                }
            }
        }

        public bool SpawnFlora(Entity entity, Rectangle worldBounds)
        {
            bool affectedTile = false;

            for (int j = 0; j < innerTiles.Count; j++)
            {
                InnerTile tile = innerTiles[j];

                if (tile.InteractableCount > 0) continue;

                affectedTile = true;

                RandomHelper random = RandomHelper.Instance;
                Rectangle keyBounds = tile.Collider.Bounds;

                var assembly = Assembly.GetExecutingAssembly();
                List<InteractableStats> types = SpawnManager.Instance.GetSpawnedTypes();

                for (int i = 0; i < types.Count; i++)
                {
                    Vector2 position = random.GetVector2(Bounds);

                    object interactable = Activator.CreateInstance(assembly.GetType("IdleCollector." + types[i].ClassName));

                    {
                        Interactable plant = (Interactable)interactable;
                        plant.Bounds = new Rectangle(position.ToPoint(), plant.Bounds.Size);
                        plant.Position = position;
                        plant.Radius = 8;
                        float yPos = position.Y + plant.Origin.Y;
                        plant.LayerDepth = (yPos - worldBounds.Y) / (float)worldBounds.Height + float.Epsilon;
                        plant.WorldDepth = worldBounds.Y;
                        plant.WorldHeight = worldBounds.Height;
                        plant.DrawColor = Color;
                        producingInteractables.Add(plant);
                    }
                }

                for (int i = 0; i < 8; i++)
                {
                    Vector2 position = random.GetVector2(keyBounds);

                    Grass grass = new Grass();
                    grass.CollisionType = CollisionType.Circle;
                    grass.Radius = 8;
                    grass.Position = position;
                    grass.Bounds = new Rectangle(position.ToPoint(), ResourceAtlas.GetRandomTileRect("grass").Size);
                    float yPos = position.Y + grass.Origin.Y;
                    grass.LayerDepth = MathHelper.Clamp((yPos - worldBounds.Y - worldBounds.Height) / ((float)worldBounds.Height * 2), 0.00001f, .9999f);
                    grass.Color = Color;
                    grass.WorldDepth = worldBounds.Y;
                    grass.WorldHeight = worldBounds.Height;

                    if (Vector2.Distance(position, entity.Position) > 35)
                        grass.Nudge(random.GetFloat(-50, 50));

                    innerTiles[j].Add(grass);
                }
            }

            return affectedTile;
        }

        public void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            for (int i = 0; i < innerTiles.Count; i++)
            {
                innerTiles[i].ApplyWind(windScroll, noise);
            }

            foreach (Interactable interactable in producingInteractables) 
                interactable.ApplyWind(windScroll, noise);
        }

        public void CheckInteractables(Entity entity)
        {
            foreach (InnerTile tile in innerTiles)
            {
                EmptyCollider interactCol = new EmptyCollider();
                interactCol.Position = entity.Position;
                interactCol.Radius = entity.InteractRange;
                interactCol.CollisionType = CollisionType.Circle;

                if (!CollisionHelper.CheckForCollision(interactCol, tile.Collider, CollisionCheck.CircleRect)) continue;

                tile.InteractWith(entity);
            }

            foreach (Interactable interactable in producingInteractables)
                interactable.InteractWith(entity);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(ResourceAtlas.TilemapAtlas, Bounds, ResourceAtlas.GetTileRect(TileType, TextureKey), color, 0, Vector2.Zero, SpriteEffects.None, layerDepth);

            foreach (Interactable interactable in producingInteractables)
                interactable.Draw(sb);

            foreach (InnerTile tile in innerTiles)
                tile.Draw(sb);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            foreach (InnerTile tile in innerTiles)
                tile.ControlledUpdate(gameTime);

            foreach (Interactable interactable in producingInteractables)
                interactable.ControlledUpdate(gameTime);

        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (InnerTile tile in innerTiles)
                tile.StandardUpdate(gameTime);

            foreach (Interactable interactable in producingInteractables)
                interactable.StandardUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            foreach (InnerTile tile in innerTiles)
                tile.SlowUpdate(gameTime);

            foreach (Interactable interactable in producingInteractables)
                interactable.SlowUpdate(gameTime);
        }
    }
}
