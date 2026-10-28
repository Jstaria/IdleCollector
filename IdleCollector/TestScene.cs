using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace IdleCollector
{
    internal sealed class TestScene : IScene
    {
        private const int TileSize = 32;
        private readonly Texture2D tileTexture;
        private readonly Rectangle worldBounds = new(-640, -480, 1280, 960);
        private readonly Player player;
        private readonly TestWorldManager worldManager;

        public float LayerDepth { get; set; }
        public Color Color { get; set; } = Color.White;

        public TestScene()
        {
            tileTexture = ResourceAtlas.GetTexture("testTile");
            player = new Player(
                ResourceAtlas.GetTexture("fox_outline"),
                new Point(240, 135),
                new Rectangle(0, 0, 64, 64),
                new Point(7, 8),
                8f)
            {
                LayerDepth = .8f,
                WorldBounds = worldBounds
            };

            worldManager = new TestWorldManager(worldBounds, new Point(288, 64));
            player.OnMove += worldManager.InteractWithFlora;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int y = worldBounds.Top; y < worldBounds.Bottom; y += TileSize)
            {
                for (int x = worldBounds.Left; x < worldBounds.Right; x += TileSize)
                    spriteBatch.Draw(tileTexture, new Rectangle(x, y, TileSize, TileSize), Color);
            }

            worldManager.Draw(spriteBatch);
            player.Draw(spriteBatch);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            player.ControlledUpdate(gameTime);
            worldManager.ControlledUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            player.StandardUpdate(gameTime);
            worldManager.StandardUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            player.SlowUpdate(gameTime);
            worldManager.SlowUpdate(gameTime);
        }
    }

    internal sealed class TestWorldManager : IScene
    {
        private const int PlotWidth = 5;
        private const int TileSize = 32;
        private readonly List<TilePiece> tiles = new();
        private readonly WindManager windManager = new();
        private readonly FastNoiseLite noise = new(1);

        public float LayerDepth { get; set; }
        public Color Color { get; set; } = Color.White;

        public TestWorldManager(Rectangle worldBounds, Point plotStart)
        {
            noise.SetFrequency(.002f);
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

            for (int y = 0; y < PlotWidth; y++)
            {
                for (int x = 0; x < PlotWidth; x++)
                {
                    Rectangle bounds = new Rectangle(plotStart + new Point(x * TileSize, y * TileSize),
                        new Point(TileSize, TileSize));
                    TilePiece tile = new TilePiece(bounds, ResourceAtlas.GetRandomAtlasKey("sand"),
                        "sand", new Point(x, y), new Color(250, 204, 158));
                    tile.LayerDepth = .01f;
                    tile.FillWithGrass(worldBounds);
                    tiles.Add(tile);
                }
            }
        }

        public void InteractWithFlora(Entity entity)
        {
            foreach (TilePiece tile in tiles)
            {
                tile.CheckInteractables(entity);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (TilePiece tile in tiles)
            {
                tile.DrawTile(spriteBatch);
                tile.Draw(spriteBatch);
            }
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            foreach (TilePiece tile in tiles)
                tile.ControlledUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (TilePiece tile in tiles)
            {
                tile.ApplyWind(windManager.TotalWindMovement, noise);
                tile.StandardUpdate(gameTime);
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            windManager.SlowUpdate(gameTime);
            foreach (TilePiece tile in tiles)
                tile.SlowUpdate(gameTime);
        }
    }
}
