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
        private int WorldSizeX;
        private int WorldSizeY;
        private float WorldNoiseFrequency;
        private int TileSize;

        private int seed = 1;

        private FastNoiseLite noise; // Cosmetic for rn
        private Random randomInstance;

        private KeyValuePair<string, Rectangle>[,] worldFloor;
        private Rectangle worldBounds;

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
            for (int j = 0; j < WorldSizeY; j++)
                for (int i = 0; i < WorldSizeX; i++)
                {
                    if (!worldFloor[i, j].Value.Intersects(Renderer.CameraBounds)) continue;

                    float noiseValue = noise.GetNoise((float)i, (float)j);
                    //Debug.WriteLine(noiseValue);
                    Color color = Color.Lerp(Color.White, Color.Brown, noiseValue / 5);
                    //if (i % 2 == 0 && j % 2 == 0)
                    //    color = Color.Red;
                    sb.Draw(ResourceAtlas.TilemapAtlas, worldFloor[i, j].Value, ResourceAtlas.GetTileRect(worldFloor[i, j].Key), color);
                }
        }

        private void Initialize()
        {
            LoadWorldData("WorldData", "SaveData");
            LoadNoise();
        }

        public void SpawnFlora(ICollidable collider)
        {
            if (collider.Radius <= 0) throw new Exception("Collider must have a radius for spawning flora!");
            Debug.WriteLine("{0} spawned flora in a radius of {1} with an area of {2} at ({3})", collider, collider.Radius, MathF.PI * collider.Radius * collider.Radius, collider.Position);
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
            worldFloor = new KeyValuePair<string, Rectangle>[WorldSizeX, WorldSizeY];

            List<string> keys = ResourceAtlas.TilemapAtlasKeys.Keys.ToList();

            for (int j = 0; j < WorldSizeX; j++)
                for (int i = 0; i < WorldSizeY; i++)
                {   
                    Rectangle position = new Rectangle(
                        (int)(i * TileSize - worldHalfX) + offset.X,
                        (int)(j * TileSize - worldHalfY) + offset.Y,
                        TileSize, TileSize);

                    string tileName = keys[randomInstance.Next(0, keys.Count)];

                    worldFloor[i, j] = new KeyValuePair<string, Rectangle>(tileName, position);
                }
        }
    }
}
