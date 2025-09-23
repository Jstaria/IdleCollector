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
        private Random randomInstance;

        private TilePiece[,] worldFloor;
        private Rectangle worldBounds;

        private CollisionTree<TilePiece> tileTree;

        public Rectangle WorldBounds { get { return worldBounds; } }
        public UpdateType Type { get; set; }

        public WorldManager()
        {
            Initialize();
        }

        public void Update(GameTime gameTime)
        {

        }

        public void Draw(SpriteBatch sb)
        {
            EmptyCollider collider = new();
            collider.Bounds = Renderer.ScaledCameraBounds;
            collider.Position = collider.Bounds.Location.ToVector2();
            collider.CollisionType = CollisionType.Rectangle;
            tileTree.GetActiveLeaves(collider, CollisionCheck.Rectangle);
            List<TilePiece> pieces = tileTree.GetCollidedWith(collider, CollisionCheck.Rectangle);

            for (int i = 0; i < pieces.Count; i++)
            {
                TilePiece piece = pieces[i];

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
        }

        public void SpawnFlora(ICollidable collider)
        {
            if (collider.Radius <= 0) throw new Exception("Collider must have a radius for spawning flora!");
            //Debug.WriteLine("{0} spawned flora in a radius of {1} with an area of {2} at ({3})", collider, collider.Radius, MathF.PI * collider.Radius * collider.Radius, collider.Position);

            List<TilePiece> tilePieces = tileTree.GetCollidedWith(collider, CollisionCheck.CircleRect);

            for (int i = 0; i < tilePieces.Count; i++)
            {
                TilePiece tp = tilePieces[i];

                tp.debugColorSwap = false;

                if (!CollisionHelper.CheckForCollision(collider, tp, CollisionCheck.CircleRect)) continue;

                tp.debugColorSwap = true;
            }
        }

        private void LoadWorldData(string name, string folder)
        {
            List<string> data = FileIO.ReadFrom(name, folder);
            FileIO.LoadDataInto(this, data);
        }

        private void LoadNoise()
        {
            randomInstance = new Random(seed);
            noise = new FastNoiseLite(seed);

            noise.SetFrequency(WorldNoiseFrequency);
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }

        public void CreateWorld()
        {
            int worldHalfX = (WorldSizeX * TileSize) / 2;
            int worldHalfY = (WorldSizeY * TileSize) / 2;
            Point offset = (Renderer.RenderSize.ToVector2() / 2).ToPoint();

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

                    float noiseValue = noise.GetNoise(i / 2, j / 2);
                    Color sandColor = new Color(250, 204, 158);
                    Color color = Color.Lerp(sandColor, Color.Brown, noiseValue / 4);

                    noiseValue = noise.GetNoise((i / 4.0f) + 100, (j / 4.0f) + 100, j);
                    color = Color.Lerp(color, Color.Gold, noiseValue / 6);

                    worldFloor[i, j] = new TilePiece(bounds, tileName, tileType, new Point(i, j), color);
                    tileTree.AddChild(worldFloor[i, j], bounds.Location);
                }
        }
    }
}
