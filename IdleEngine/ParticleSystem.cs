using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public delegate Vector2 GetVector();

    public struct ParticleSystemStats
    {
        public Rectangle Position;
        public GetVector TrackPosition;
        public int MaxParticleCount;
        public Color[] ParticleStartColor;
        public Color[] ParticleEndColor;
        public Texture2D ParticleTexture;
        public float[] ParticleSpeed;
        public float[] SpreadAngle;
        public float[] ParticleRotationSpeed;
    }

    public class ParticleSystem
    {
        private List<Particle> particles;
        
        private ParticleSystemStats stats;

        /// <summary>
        /// Simple particle system
        /// </summary>
        /// <param name="particleAmount"></param>
        /// <param name="startColor"></param>
        /// <param name="endColor"></param>
        /// <param name="decaySpeed">The speed the particles will fade away at</param>
        /// <param name="asset">Particle texture</param>
        /// <param name="position">Position of the system</param>
        /// <param name="speed">Speed of the particles from their source</param>
        /// <param name="doesRotate">Do the particles rotate as they move</param>
        /// <param name="spreadAngle">Currently only supports 0 degrees or 360 degrees</param>
        public ParticleSystem(int particleAmount, Color startColor, Color endColor, float decaySpeed, Texture2D asset, Rectangle position, int speed, bool doesRotate, float spreadAngle)
        {
            this.particleAmount = particleAmount;
            this.decaySpeed = decaySpeed;
            this.asset = asset;
            this.startColor = startColor;
            this.endColor = endColor;
            this.speed = speed;

            this.position = position;
            particles = new List<Particle>();
            random = new Random();
            this.doesRotate = doesRotate;
            rotationSpeed = 0;
            this.spreadAngle = spreadAngle;

            // Spawning amount is based on how many particles are allowed in the system
            // Even with this simple formula, the higher numbers will never fill a list
            // but it will get pretty laggy past 100,000 particles allowed (this is with a Ryzen 7 3700X) considering this is all CPU bound
            // and that I couldn't offload anything to the GPU bc HLSL is outdated and not even chat gpt can help me with it, well... sort of
            spawnAmount = particleAmount * .005f;
        }

        public void Update()
        {
            // Basic update information to cull list of non relevant particles
            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].Update();

                if (particles[i].LifeSpan == 0)
                {
                    particles.RemoveAt(i);

                    i--;
                }
            }

            /* new Vector2(
                    (float)(Math.Cos(MathHelper.ToRadians(spreadAngle)) - Math.Sin(MathHelper.ToRadians(spreadAngle))) * speed,
                    (float)(Math.Sin(MathHelper.ToRadians(spreadAngle)) + Math.Cos(MathHelper.ToRadians(spreadAngle))) * speed);
            */

            // If list is not full, another will be spawned
            if (particles.Count < particleAmount)
            {
                if (doesRotate)
                {
                    ParticleStats stats = new ParticleStats();
                    stats.Position = RandomHelper.Instance.GetVector2(position);
                    stats.RotationSpeed = RandomHelper.Instance.GetFloat(-speed, speed);

                    rotationSpeed = random.Next(-5, 5) * .01f;

                    for (int i = 0; i < spawnAmount; i++)
                    {
                        particles.Add(new Particle(, ), random.Next((int)(decaySpeed * 100 - 10), (int)(decaySpeed * 100)) * .01f, asset, startColor, endColor, speed, rotationSpeed, random.Next((int)spreadAngle), doesRotate));
                    }
                }

                else
                {
                    particles.Add(new Particle(new Vector2(random.Next(position.X, position.X + position.Width + 1), random.Next(position.Y, position.Y + position.Height + 1)), decaySpeed, asset, startColor, endColor, speed, random.Next((int)spreadAngle)));
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            foreach (Particle particle in particles)
            {
                particle.DrawAsset(sb);
            }
        }

        /// <summary>
        /// Resets particle list
        /// </summary>
        public void Reset()
        {
            particles.Clear();
        }
    }
}
