using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IdleCollector
{
    internal class Grass : Interactable
    {
        private Color WaveColor;
        private Color SpawnColor;
        private Color InvWaveColor;
        private bool isSpawningColor = true;
        private float spawnColorElapsed;
        private const float SpawnColorDuration = .15f;
        private const float SpawnColorFollowRate = 14f;
        private Color[] touched = new Color[] { new Color(166, 160, 98), new Color(166, 160, 98) };
        private float coolDown = 1;
        private bool playGrass;
        private bool prevGrass;
        public override Vector2 Origin { get => new Vector2(Bounds.Width / 2, Bounds.Height / 2); }
        public float CoolDown => coolDown;

        public Grass() : base()
        {
            tileType = "grass";
            textureKey = ResourceAtlas.GetRandomAtlasKey("grass");

            posSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.5f, /*Resting Position*/0);
            rotSpring = new Spring(/*Angular Frequency*/10, /*Damping Ratio*/.2f, /*Resting Position*/0);
            rotationAmt = MathHelper.ToRadians(45);
            xOffsetAmt = RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One);

            textureSourceRect = ResourceAtlas.GetTileRect(tileType, textureKey);

            //if (RandomHelper.Instance.GetDouble() < .5f)
                SpawnColor = Color.White;
            //else isSpawningColor = false;
        }

        public override void StandardUpdate(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (coolDown > 0)
                coolDown -= elapsedSeconds;

            if (isSpawningColor)
            {
                float blend = 1f - MathF.Exp(-SpawnColorFollowRate * elapsedSeconds);
                SpawnColor = Color.Lerp(SpawnColor, DrawColor, blend);
                spawnColorElapsed += elapsedSeconds;

                if (spawnColorElapsed >= SpawnColorDuration)
                {
                    SpawnColor = DrawColor;
                    isSpawningColor = false;
                }
            }

            posSpring.Update();
            rotSpring.Update();

            Rotation = rotSpring.Position * rotationAmt;
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
        }
        public override void SlowUpdate(GameTime gameTime)
        {
        }

        public override void Draw(SpriteBatch sb)
        {
            Vector2 offset = xOffsetAmt * posSpring.Position;
            Rectangle rect = new Rectangle(Bounds.Location + offset.ToPoint(), Bounds.Size);

            float yPos = Position.Y + offset.Y + Origin.Y * 2 + Rotation;
            LayerDepth = WorldManager.GetLayerDepth(yPos);

            sb.Draw(ResourceAtlas.TilemapAtlas, rect, textureSourceRect,
                isSpawningColor ? SpawnColor : DrawColor, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }

        public override void InteractWith(Entity collider)
        {
            SetRotation(collider, 25, .5f, true);

            playGrass = Vector2.DistanceSquared(collider.Position, Position) < 25;

            if (playGrass && !prevGrass)
            {
                AudioController.Instance.PlaySoundEffect("grass" + RandomHelper.Instance.GetInt(8, 11), "soundEffectVolume", RandomHelper.Instance.GetFloat(-.5f, .5f));
                AudioController.Instance.PlaySoundEffect("grass" + RandomHelper.Instance.GetInt(1, 7), "soundEffectVolume", RandomHelper.Instance.GetFloat(-.5f, .5f));
            }

            prevGrass = playGrass;
        }
        public override void SecondaryInteractWith(Entity collider)
        {
            Color = RandomHelper.Instance.GetColor(touched[0], touched[1]);
        }

        public override void Nudge(float strength)
        {
            posSpring.Nudge(strength);
            rotSpring.Nudge(strength);
        }

        public override void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            float value = .1f;
            WaveColor = new Color(Color.ToVector3() + new Vector3(value));

            float noiseValue = noise.GetNoise(Position.X + windScroll.X, Position.Y + windScroll.Y);
            rotSpring.Nudge(noiseValue * .75f);

            DrawColor = Color.Lerp(WaveColor, Color, ((noiseValue + 1) / 2));
        }

    }
}
