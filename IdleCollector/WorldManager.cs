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
    internal class WorldManager: IScene
    {
        private int WorldSizeX;
        private int WorldSizeY;
        private int WorldNoiseFrequency;
        private int TileSize;

        private int seed = 0;

        private FastNoiseLite noise; // Cosmetic for rn
        private Random randomInstance;

        private string[,] worldFloor;
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
                    Rectangle position = new Rectangle(i * TileSize - TileSize * WorldSizeX / 2, j * TileSize - TileSize * WorldSizeY / 2, TileSize, TileSize);
                    Color color = Color.White * noise.GetNoise(i, j);
                    sb.Draw(ResourceAtlas.TilemapAtlas, position, ResourceAtlas.GetTileRect(worldFloor[i, j]), color);
                }
        }

        private void Initialize()
        {
            LoadWorldData("WorldData", "SaveData");
            LoadNoise();
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
            int worldHalfX = WorldSizeX / 2;
            int worldHalfY = WorldSizeY / 2;

            worldBounds = new Rectangle(32 * worldHalfX, 32 * worldHalfY, 32 * WorldSizeX, 32 * WorldSizeY);
            worldFloor = new string[WorldSizeX, WorldSizeY];

            List<string> keys = ResourceAtlas.TilemapAtlasKeys.Keys.ToList();

            for (int j = 0; j < WorldSizeX; j++)
                for (int i = 0; i < WorldSizeY; i++)
                {
                    string tileName = keys[randomInstance.Next(0, keys.Count)];

                    worldFloor[i, j] = tileName;
                }
        }
    }
}
