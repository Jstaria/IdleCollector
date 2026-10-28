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
        private readonly ParticleSystem textParticles;
        private const float TextEmitInterval = .6f;
        private float textEmitTimer = TextEmitInterval;
        private int textCount;

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

            textParticles = new ParticleSystem(new ParticleSystemStats
            {
                FontName = "DePixelHalbfettTitle",
                TextShadowColor = Color.Black,
                TextShadowOffset = new Vector2(3f, 3f),
                TrackPosition = () => new Vector2(160, 135),
                TrackLayerDepth = () => .9f,
                SpawnBounds = new[] { new[] { new Rectangle(-4, -2, 8, 4) } },
                StartingVelocity = new[] { new Vector2(-.35f, -1.8f), new Vector2(.35f, -2.2f) },
                ActingForce = _ => new Vector2(0f, .05f),
                ParticleSpeed = new[] { 1f },
                ParticleLifeSpan = new[] { 1.6f },
                ParticleDespawnDistance = 500f,
                ParticleRotation = new[] { MathHelper.ToRadians(-12f), MathHelper.ToRadians(12f) },
                ParticleSize = new[] { .35f },
                ParticleSizeDecayRate = _ => 1f,
                ParticleStartColor = new[] { Color.White },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleColorDecayRate = t => MathHelper.Clamp((t - .65f) / .35f, 0f, 1f),
                EmitRate = new[] { TextEmitInterval },
                EmitCount = new[] { 1 },
                MaxParticleCount = 10,
                ResetParticlesAfterDeath = false
            });
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
            textParticles.Draw(spriteBatch);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            player.ControlledUpdate(gameTime);
            worldManager.ControlledUpdate(gameTime);
            textParticles.ControlledUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            player.StandardUpdate(gameTime);
            worldManager.StandardUpdate(gameTime);
            textParticles.StandardUpdate(gameTime);

            textEmitTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (textEmitTimer >= TextEmitInterval)
            {
                textEmitTimer -= TextEmitInterval;
                string countText = (++textCount).ToString(System.Globalization.CultureInfo.InvariantCulture);
                textParticles.EmitText($"<fx 1,1,0,0,0>{countText}</fx>");
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            player.SlowUpdate(gameTime);
            worldManager.SlowUpdate(gameTime);
            textParticles.SlowUpdate(gameTime);
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
