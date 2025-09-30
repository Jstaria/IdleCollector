using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class WorldManager : IScene
    {
        private int WorldTreeDepth = 1;
        private int WorldSizeX = 10;
        private int WorldSizeY = 10;
        private float WorldNoiseFrequency = .01f;
        private int TileSize = 32;

        private int seed = 1;

        private FastNoiseLite noise; // Cosmetic for rn
        private RandomHelper random;
        private WindManager windManager;

        private TilePiece[,] worldFloor;
        private static Rectangle worldBounds;

        private CollisionTree<TilePiece> tileTree;
        private List<TilePiece> activeTiles;
        public Rectangle WorldBounds { get { return worldBounds; } }
        public UpdateType Type { get; set; }
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public WorldManager()
        {
            Initialize();

            Type = UpdateType.Controlled;
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            EmptyCollider collider = new();
            collider.Bounds = Renderer.ScaledCameraBounds;
            collider.Position = collider.Bounds.Location.ToVector2();
            collider.CollisionType = CollisionType.Rectangle;

            tileTree.GetActiveLeaves(collider, CollisionCheck.Rectangle);
            activeTiles = tileTree.GetCollidedWith(collider, CollisionCheck.Rectangle);

            for (int i = 0; i < activeTiles.Count; i++)
            {
                TilePiece piece = activeTiles[i];

                if (!piece.Bounds.Intersects(Renderer.ScaledCameraBounds)) continue;

                piece.ControlledUpdate(gameTime);
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            windManager.SlowUpdate(gameTime);

            if (activeTiles == null) return;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                TilePiece piece = activeTiles[i];
                piece.SlowUpdate(gameTime);
            }
        }

        public void StandardUpdate(GameTime gameTime)
        {
            if (activeTiles == null) return;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                TilePiece piece = activeTiles[i];

                if (!piece.Bounds.Intersects(Renderer.ScaledCameraBounds)) continue;

                piece.ApplyWind(windManager.TotalWindMovement, noise);
                piece.StandardUpdate(gameTime);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (activeTiles == null) return;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                TilePiece piece = activeTiles[i];

                if (!piece.Bounds.Intersects(Renderer.ScaledCameraBounds)) continue;

                piece.Draw(sb);
            }

            //if (tileTree != null)
            //{
            //    tileTree.DrawActiveBounds(sb);
            //}
        }

        private void Initialize()
        {
            LoadWorldData("WorldData", "SaveData");
            LoadNoise();
            CreateWorld();
        }

        public void InteractWithFlora(ICollidable collider)
        {
            List<TilePiece> tilePieces = tileTree.GetCollidedWith(collider, CollisionCheck.CircleRect);

            for (int i = 0; i < tilePieces.Count; i++)
            {
                TilePiece tp = tilePieces[i];

                tp.CheckInteractables(collider);
            }
        }

        public void SpawnFlora(ICollidable collider)
        {
            if (collider.Radius <= 0) throw new Exception("Collider must have a radius for spawning flora!");
            //Debug.WriteLine("{0} spawned flora in a radius of {1} with an area of {2} at ({3})", collider, collider.Radius, MathF.PI * collider.Radius * collider.Radius, collider.Position);

            List<TilePiece> tilePieces = tileTree.GetCollidedWith(collider, CollisionCheck.CircleRect);

            for (int i = 0; i < tilePieces.Count; i++)
            {
                TilePiece tp = tilePieces[i];

                if (!CollisionHelper.CheckForCollision(collider, tp, CollisionCheck.CircleRect)) continue;

                tp.SpawnFlora(collider, worldBounds);
            }
        }

        private void LoadWorldData(string name, string folder)
        {
            List<string> data = FileIO.ReadFrom(name, folder);
            FileIO.LoadDataInto(this, data);
        }

        private void LoadNoise()
        {
            random = RandomHelper.Instance;
            random.SetSeed(2);
            noise = new FastNoiseLite(random.GetSeed());

            noise.SetFrequency(WorldNoiseFrequency);
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }

        public void CreateWorld()
        {
            int worldHalfX = (WorldSizeX * TileSize) / 2;
            int worldHalfY = (WorldSizeY * TileSize) / 2;
            Point offset = (Renderer.RenderSize.ToVector2() / 2).ToPoint();

            windManager = new WindManager();
            worldBounds = new Rectangle(-worldHalfX + offset.X, -worldHalfY + offset.Y, TileSize * WorldSizeX, TileSize * WorldSizeY);
            worldFloor = new TilePiece[WorldSizeX, WorldSizeY];

            tileTree = new CollisionTree<TilePiece>(WorldBounds, WorldTreeDepth);

            for (int j = 0; j < WorldSizeX; j++)
                for (int i = 0; i < WorldSizeY; i++)
                {
                    Rectangle bounds = new Rectangle(
                        (int)(i * TileSize - worldHalfX) + offset.X,
                        (int)(j * TileSize - worldHalfY) + offset.Y,
                        TileSize, TileSize);
                    string tileType = "sand";
                    string tileName = ResourceAtlas.GetRandomAtlasKey(tileType);

                    float noiseValue = noise.GetNoise(bounds.Location.X / 2, bounds.Location.Y / 2);
                    Color sandColor = new Color(250, 204, 158);
                    Color color = Color.Lerp(sandColor, Color.Brown, noiseValue / 4);

                    noiseValue = noise.GetNoise((bounds.Location.X / 4 + 1000), (bounds.Location.Y / 4 + 1000));
                    color = Color.Lerp(color, Color.Gold, noiseValue / 10);

                    worldFloor[i, j] = new TilePiece(bounds, tileName, tileType, new Point(i, j), color);
                    worldFloor[i, j].LayerDepth = 0.0f;
                    tileTree.AddChild(worldFloor[i, j], bounds.Location);
                }
        }

        public void ChangePlayerLayerDepth(Player player)
        {
            float yPos = player.Position.Y + player.Origin.Y; /*- player.Bounds.Height / 8;*/
            player.LayerDepth = WorldManager.GetLayerDepth(yPos);
        }

        public static float GetLayerDepth(float yPos)
        {
            return (yPos - worldBounds.Y + 100) / ((float)worldBounds.Height * 4) + float.Epsilon;
        }
    }
}
