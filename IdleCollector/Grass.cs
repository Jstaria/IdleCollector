using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private const float SpawnColorDuration = .2f;
        private const float SpawnColorFollowRate = 14f;
        private Color[] touchedColor = new Color[] { new Color(166, 160, 98), new Color(166, 160, 98) };
        private float coolDown = 1;
        private float positionNoiseValue;
        private float sizeScaler = 1f; // applied in applycolor()
        private bool playGrass;
        private bool prevGrass;
        private float hue;

        public override Vector2 Origin { get => new Vector2(Bounds.Width / 2, Bounds.Height / 2); }
        public float CoolDown => coolDown;
        public float Size { get; set; } = 1f;

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
            //hue = (float)gameTime.TotalGameTime.TotalSeconds * 20;
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
            float yPos = Position.Y + offset.Y + Origin.Y * 2 + Rotation;
            LayerDepth = WorldManager.GetLayerDepth(yPos);

            sb.Draw(ResourceAtlas.TilemapAtlas, Position + offset, textureSourceRect,
                isSpawningColor ? SpawnColor : DrawColor, Rotation, Origin, Size * sizeScaler, SpriteEffects.None, LayerDepth);
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
            Color = RandomHelper.Instance.GetColor(touchedColor[0], touchedColor[1]);
        }

        private static Color AdjustSaturationFromNoise(Color color, float noiseValue, float amount = .35f)
        {
            Vector3 rgb = color.ToVector3();
            float max = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
            float min = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
            float delta = max - min;
            float hue = 0;

            if (delta > 0)
            {
                if (max == rgb.X)
                    hue = ((rgb.Y - rgb.Z) / delta) % 6f;
                else if (max == rgb.Y)
                    hue = (rgb.Z - rgb.X) / delta + 2f;
                else
                    hue = (rgb.X - rgb.Y) / delta + 4f;

                hue = (hue / 6f + 1f) % 1f;
            }

            float saturation = max <= 0 ? 0 : delta / max;
            saturation = MathHelper.Clamp(saturation * (1 + MathHelper.Clamp(noiseValue, -1, 1) * amount), 0, 1);

            float chroma = max * saturation;
            float hueSection = hue * 6f;
            float secondary = chroma * (1 - Math.Abs(hueSection % 2f - 1));
            float match = max - chroma;
            Vector3 adjustedRgb;

            if (hueSection < 1)
                adjustedRgb = new Vector3(chroma, secondary, 0);
            else if (hueSection < 2)
                adjustedRgb = new Vector3(secondary, chroma, 0);
            else if (hueSection < 3)
                adjustedRgb = new Vector3(0, chroma, secondary);
            else if (hueSection < 4)
                adjustedRgb = new Vector3(0, secondary, chroma);
            else if (hueSection < 5)
                adjustedRgb = new Vector3(secondary, 0, chroma);
            else
                adjustedRgb = new Vector3(chroma, 0, secondary);

            adjustedRgb += new Vector3(match);
            float adjustedMax = Math.Max(adjustedRgb.X, Math.Max(adjustedRgb.Y, adjustedRgb.Z));
            if (adjustedMax > 0)
                adjustedRgb *= max / adjustedMax;

            Color adjustedColor = new Color(adjustedRgb);
            adjustedColor.A = color.A;
            return adjustedColor;
        }

        public override void Nudge(float strength)
        {
            posSpring.Nudge(strength);
            rotSpring.Nudge(strength);
        }

        public Color getRGB(int H, double S = 1, double V = 1)
        {
            H %= 360;
            H = Math.Abs(H);

            double dC = (V * S);
            double Hd = ((double)H) / 60;
            double dX = (dC * (1 - Math.Abs((Hd % 2) - 1)));//dC * (1 - ((Hd + 1) % 2));

            int C = (int)(dC * 255);
            int X = (int)(dX * 255);

            //Console.WriteLine("H:" + H + " S:" + S + " V:" + V + ", C: " + C + " X:" + X + " Hd:" + Hd);

            if (Hd < 1)
            {
                return new Color(C, X, 0);
            }
            else if (Hd < 2)
            {
                return new Color(X, C, 0);
            }
            else if (Hd < 3)
            {
                return new Color(0, C, X);
            }
            else if (Hd < 4)
            {
                return new Color(0, X, C);
            }
            else if (Hd < 5)
            {
                return new Color(X, 0, C);
            }
            else if (Hd < 6)
            {
                return new Color(C, 0, X);
            }
            return new Color(0, 0, 0);
        }

        public override void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            float value = .1f;
            WaveColor = new Color(Color.ToVector3() + new Vector3(value));

            float noiseValue = noise.GetNoise(Position.X + windScroll.X, Position.Y + windScroll.Y);
            rotSpring.Nudge(noiseValue * .75f);

            DrawColor = Color.Lerp(WaveColor, Color, ((noiseValue + 1) / 2));
        }

        public void ApplyColor(Color color)
        {
            positionNoiseValue = RandomHelper.Instance.GetFloatNoise(Position * 2, FastNoiseLite.NoiseType.Cellular);
            sizeScaler = MathHelper.Lerp(1f, 1.75f, (positionNoiseValue + 1f) / 2f);

            touchedColor[1] = AdjustSaturationFromNoise(touchedColor[1], positionNoiseValue);
            Color = AdjustSaturationFromNoise(color, positionNoiseValue);
        }
    }
}
