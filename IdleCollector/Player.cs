using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IdleCollector
{
    internal class Player : Entity
    {
        private ParticleSystem walkParticles;
        private Dictionary<Vector2, KeyValuePair<int, Rectangle[]>> playerWalkBounds;
        private Rectangle[] currentWalkBounds;
        private Texture2D shadow;
        private bool wasWalking;
        private int lastWalkingFrameY;

        public Player(Texture2D spriteSheet, Point position, Rectangle bounds, Point frameCount, float frameSpeed)
        {
            this.spriteSheet = spriteSheet;
            this.Position = position.ToVector2();
            this.Bounds = bounds;
            this.FrameCount = frameCount;
            this.Type = UpdateType.Controlled;
            this.CollisionType = CollisionType.Circle;
            this.Origin = new Vector2(bounds.Width / 2, bounds.Height * .65f);
            this.frameSpeed = frameSpeed;
            LoadPlayerData("PlayerData", "SaveData");
            shadow = ResourceAtlas.GetTexture("shadow");
        }

        public override void ControlledUpdate(GameTime gameTime)
        {
            GetInput();
            ClampPosition();
            OnPositionSpawnFlora(gameTime);
            InvokeOnMove(this);

            walkParticles.ControlledUpdate(gameTime);
        }

        public override void StandardUpdate(GameTime gameTime)
        {
            walkParticles.StandardUpdate(gameTime);
        }

        public override void SlowUpdate(GameTime gameTime)
        {
            walkParticles.SlowUpdate(gameTime);
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Draw(spriteSheet, new Rectangle(Position.ToPoint(), Bounds.Size), new Rectangle(64 * CurrentFrame.X, 64 * CurrentFrame.Y, 64, 64), Color.White, 0, Origin, SpriteEffects.None, LayerDepth);
            sb.Draw(shadow, new Rectangle(Position.ToPoint(), Bounds.Size), null, Color.White, 0, Origin, SpriteEffects.None, LayerDepth - .0005f);

            if (currentWalkBounds == null) return;
            //foreach (Rectangle rect in walkParticles.GetCurrentSpawnBounds())
            //{
            //    sb.DrawRect(rect, 2, Color.Red);
            //}

            walkParticles.Draw(sb);
        }

        private void OnPositionSpawnFlora(GameTime gameTime)
        {
            prevSpawnTime++;

            if (prevSpawnTime < spawnFrequency * 60) return;

            prevSpawnTime = 0;
            InvokeOnSpawn(this);
        }

        private void ClampPosition()
        {
            Vector2 position = Vector2.Zero;

            if (WorldBounds == Rectangle.Empty) return;

            position.X = Math.Clamp(Position.X, WorldBounds.Left + Bounds.Width / 6, WorldBounds.Right - Bounds.Width / 6);
            position.Y = Math.Clamp(Position.Y, WorldBounds.Top + Bounds.Height / 6, WorldBounds.Bottom - Bounds.Height / 6);

            Position = position;
        }

        private void GetInput()
        {
            Vector2 direction = Vector2.Zero;
            bool[] keyBools = new bool[4];

            if (keyBools[0] = Input.IsButtonDown(Keys.W)) direction.Y--;
            if (keyBools[1] = Input.IsButtonDown(Keys.A)) direction.X--;
            if (keyBools[2] = Input.IsButtonDown(Keys.S)) direction.Y++;
            if (keyBools[3] = Input.IsButtonDown(Keys.D)) direction.X++;

            SetSpriteDirection(keyBools);

            //if (direction != Vector2.Zero)
            //    direction.Normalize();

            KeyValuePair<int, Rectangle[]> pair = playerWalkBounds[direction];
            currentWalkBounds = pair.Value;
            walkParticles.SetCurrentSpawnBounds(pair.Key);

            Vector2 offset = new Vector2(0, -1);

            if (direction.Y < 0)
                offset = new Vector2(0, direction.Y);
            if (direction.Y > 0)
                offset = Vector2.Zero;

            if (velocity.Length() > 0)
                walkParticles.SetStartingVelocity(new Vector2[] { -direction / 2 + offset, -direction + offset * 2 });

            velocity = direction * speed;

            Move(velocity);
        }

        private void SetSpriteDirection(bool[] bools)
        {

            wasWalking = bools.Contains(true);

            if (wasWalking)
            {
                Vector2 betweenFrame = this.InBetweenFrame;
                betweenFrame.X += FrameSpeed;
                InBetweenFrame = betweenFrame;

                float CurrentFrameX = 1 + ((betweenFrame.X % (FrameCount.X - 1)));

                bool up = bools[0];
                bool left = bools[1];
                bool down = bools[2];
                bool right = bools[3];

                // Resolve opposing inputs
                bool verticalUp = up && !down;
                bool verticalDown = down && !up;
                bool horizontalLeft = left && !right;
                bool horizontalRight = right && !left;

                // 8-direction logic
                if (verticalUp && horizontalLeft)
                    SetFrame((int)CurrentFrameX, 4);      // Up-Left
                else if (verticalUp && horizontalRight)
                    SetFrame((int)CurrentFrameX, 5);      // Up-Right
                else if (verticalDown && horizontalLeft)
                    SetFrame((int)CurrentFrameX, 6);      // Down-Left
                else if (verticalDown && horizontalRight)
                    SetFrame((int)CurrentFrameX, 7);      // Down-Right
                else if (verticalUp)
                    SetFrame((int)CurrentFrameX, 2);      // Up
                else if (verticalDown)
                    SetFrame((int)CurrentFrameX, 3);      // Down
                else if (horizontalLeft)
                    SetFrame((int)CurrentFrameX, 0);      // Left
                else if (horizontalRight)
                    SetFrame((int)CurrentFrameX, 1);      // Right
            }
            else
            {
                SetFrame(0, lastWalkingFrameY);
            }
            
            lastWalkingFrameY = (int)CurrentFrame.Y;
        }

        private void LoadPlayerData(string name, string folder)
        {
            List<string> data = FileIO.ReadFrom(name, folder);
            FileIO.LoadDataInto(this, data);

            // This could be loaded in using relfection but that can be done later
            LoadWalkingBounds();
            InitializeParticles();
        }

        private void LoadWalkingBounds()
        {
            playerWalkBounds = new();
            Rectangle[] vertical = new Rectangle[] { new Rectangle(25, 32, 14, 16) };
            Rectangle[] diagonalTR = new Rectangle[] { new Rectangle(20, 43, 13, 5), new Rectangle(28, 35, 13, 5) };
            Rectangle[] horizontal = new Rectangle[] { new Rectangle(21, 42, 22, 6) };
            Rectangle[] diagonalTL = new Rectangle[] { new Rectangle(24, 35, 13, 5), new Rectangle(32, 43, 13, 5) };

            playerWalkBounds.Add(Vector2.UnitY, new KeyValuePair<int, Rectangle[]>(0, vertical));
            playerWalkBounds.Add(Vector2.UnitX, new KeyValuePair<int, Rectangle[]>(1, horizontal));
            playerWalkBounds.Add(-Vector2.UnitY, new KeyValuePair<int, Rectangle[]>(2, vertical));
            playerWalkBounds.Add(-Vector2.UnitX, new KeyValuePair<int, Rectangle[]>(3, horizontal));
            playerWalkBounds.Add(Vector2.One, new KeyValuePair<int, Rectangle[]>(4, diagonalTL));
            playerWalkBounds.Add(new Vector2(1, -1), new KeyValuePair<int, Rectangle[]>(5, diagonalTR));
            playerWalkBounds.Add(-Vector2.One, new KeyValuePair<int, Rectangle[]>(6, diagonalTL));
            playerWalkBounds.Add(-new Vector2(1, -1), new KeyValuePair<int, Rectangle[]>(7, diagonalTR));
            playerWalkBounds.Add(new Vector2(0, 0), new KeyValuePair<int, Rectangle[]>(8, new Rectangle[] { new Rectangle(4000, 0, 10, 10) }));
        }

        private void InitializeParticles()
        {
            ParticleSystemStats stats = new ParticleSystemStats();

            stats.StartingVelocity = new Vector2[] { new Vector2(0, -1f) };
            stats.ActingForce = (t) => new Vector2(0, .1f);
            stats.ParticleSize = new float[] { 1f, 2f };
            stats.ParticleSpeed = new float[] { .5f, 1 };
            stats.EmitRate = new float[] { FrameSpeed * 2 };
            stats.EmitCount = new int[] { 1 };
            stats.ParticleStartColor = new Color[] { new Color(117, 188, 255), new Color(0, 100, 194) };
            stats.ParticleEndColor = new Color[] { new Color(117, 188, 255) * 0f, new Color(0, 100, 194) * 0f };
            stats.MaxParticleCount = 10;
            stats.ParticleColorDecayRate += (float t) => MathF.Pow(t, .5f);
            stats.ParticleSizeDecayRate += (float t) => 1;
            stats.ParticleDespawnDistance = 5000;
            stats.TrackLayerDepth += () => LayerDepth - .0005f;
            stats.TrackPosition += () => Position - Origin;
            stats.ParticleTextureKeys = new string[] { "dust1", "dust2", "dust3", "dust4", "dust5", "dust6", "dust7" };

            Point renderSize = Renderer.RenderSize;
            int width = 100;
            int height = 100;
            int xOffset = renderSize.X / 2;
            int yOffset = renderSize.Y / 2;

            Rectangle[] bounds = new Rectangle[]
            {
                new Rectangle(-width - xOffset, -yOffset , width, renderSize.Y),
                new Rectangle(-xOffset, -height - yOffset , renderSize.X, height),
                new Rectangle(renderSize.X - xOffset, -yOffset , width, renderSize.Y),
                new Rectangle(-xOffset, renderSize.Y - yOffset , renderSize.X, height)
            };
            stats.SpawnBounds = playerWalkBounds.Values
                .Select(pair => pair.Value.Select(r => new Rectangle(r.Location, r.Size)).ToArray())
                .ToArray();
            stats.ParticleRotation = new float[] { 0, 5 };
            float rot = RandomHelper.Instance.GetFloat(0.1f, .2f);
            stats.ParticleRotationSpeed = (t) => rot;
            stats.ParticleLifeSpan = new float[] { .5f };

            walkParticles = new ParticleSystem(stats);
        }
    }
}
