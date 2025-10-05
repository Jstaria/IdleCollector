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
    public delegate float GetFloat();

    public struct ParticleSystemStats
    {
        public Vector2[] StartingVelocity;
        public Rectangle[][] SpawnBounds;
        public int CurrentBounds;
        public bool UseRandomBounds;
        public float[] ParticleLifeSpan;
        public GetVector TrackPosition;
        public GetVector ActingForce;
        public float ParticleDespawnDistance;
        public GetFloat TrackLayerDepth;
        public int MaxParticleCount;
        public Color[] ParticleStartColor;
        public Color[] ParticleEndColor;
        public string[] ParticleTextureKeys;
        public float[] ParticleSpeed;
        public float[] ParticleRotationSpeed;
        public float[] ParticleRotation;
        public float[] ParticleSize;
        public float[] EmitRate;
        public int[] EmitCount;

        public SpriteFont Font;
        public string ParticleText;

        public Curve ParticleColorDecayRate;
        public Curve ParticleSizeDecayRate;
    }

    public class ParticleSystem : IRenderable, IUpdatable
    {
        private List<Particle> particles;
        private List<int> particleIndices;
        private ParticleSystemStats stats;
        private Rectangle[][] bounds;
        private float emitWaitTime;

        public ParticleSystem(ParticleSystemStats stats)
        {
            particleIndices = new List<int>();
            particles = new List<Particle>();   

            this.stats = stats;

            bounds = new Rectangle[stats.SpawnBounds.GetLength(0)][];

            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i] = new Rectangle[stats.SpawnBounds[i].Length];
                for (int j = 0; j < stats.SpawnBounds[i].Length; j++)
                {
                    bounds[i][j] = new Rectangle(stats.SpawnBounds[i][j].Location, stats.SpawnBounds[i][j].Size);
                }
            }
        }

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public void ControlledUpdate(GameTime gameTime)
        {
            foreach (Particle particle in particles)
            {
                particle.ControlledUpdate(gameTime);
            }

            if (particles.Count < stats.MaxParticleCount && emitWaitTime <= 0)
            {
                EmitParticle();
            }

            emitWaitTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void SlowUpdate(GameTime gameTime)
        {
            foreach(Particle particle in particles)
                particle.SlowUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                Particle particle = particles[i];
                particle.StandardUpdate(gameTime);

                if (particle.LifeSpan <= 0)
                    particleIndices.Add(i);
                
                if (Vector2.Distance(particle.position, stats.TrackPosition.Invoke()) > stats.ParticleDespawnDistance)
                    particleIndices.Add(i);
            }

            UpdateBounds();
            CullParticles();
        }

        public void Draw(SpriteBatch sb)
        {
            foreach (Particle particle in particles)
                particle.Draw(sb);
        }

        private void UpdateBounds()
        {
            if (stats.TrackPosition == null) return;

            for(int i = 0; i < bounds.Length; i++)
            {
                for (int j = 0; j < bounds[i].Length; j++)
                {
                    bounds[i][j].Location = stats.SpawnBounds[i][j].Location + stats.TrackPosition.Invoke().ToPoint();
                }
            }
        }

        private void EmitParticle()
        {
            emitWaitTime = stats.EmitRate.Length == 1 ? stats.EmitRate[0] : RandomHelper.Instance.GetFloat(stats.EmitRate[0], stats.EmitRate[1]);

            int count = stats.EmitCount.Length == 1 ? stats.EmitCount[0] : RandomHelper.Instance.GetInt(stats.EmitCount[0], stats.EmitCount[1]);

            for (int i = 0; i < count; i++)
            {
                ParticleStats particleStats = new ParticleStats();

                particleStats.ColorDecayRate = stats.ParticleColorDecayRate;
                particleStats.SizeDecayRate = stats.ParticleSizeDecayRate;

                particleStats.StartColor = stats.ParticleStartColor.Length == 1 ?
                    stats.ParticleStartColor[0] :
                    RandomHelper.Instance.GetColor(stats.ParticleStartColor[0], stats.ParticleStartColor[1]);

                particleStats.EndColor = stats.ParticleEndColor.Length == 1 ?
                    stats.ParticleEndColor[0] :
                    RandomHelper.Instance.GetColor(stats.ParticleEndColor[0], stats.ParticleEndColor[1]);

                particleStats.Position += () =>
                {
                    int ind = stats.CurrentBounds;
                    int ind2 = 0;

                    if (stats.UseRandomBounds)
                        ind = RandomHelper.Instance.GetIntExclusive(0, bounds.Length);

                    if (bounds[ind].Length != 0)
                        ind2 = RandomHelper.Instance.GetIntExclusive(0, bounds[ind].Length);

                    return RandomHelper.Instance.GetVector2(bounds[ind][ind2]);
                };

                if (stats.ParticleTextureKeys != null)
                    particleStats.TextureKey = stats.ParticleTextureKeys[RandomHelper.Instance.GetIntExclusive(0, stats.ParticleTextureKeys.Length)];
                if (stats.ParticleSize != null)
                    particleStats.Size = stats.ParticleSize.Length == 1 ? stats.ParticleSize[0] : RandomHelper.Instance.GetFloat(stats.ParticleSize[0], stats.ParticleSize[1]);
                if (stats.ParticleRotationSpeed != null)
                    particleStats.RotationSpeed = stats.ParticleRotationSpeed.Length == 1 ? stats.ParticleRotationSpeed[0] : RandomHelper.Instance.GetFloat(stats.ParticleRotationSpeed[0], stats.ParticleRotationSpeed[1]);
                if (stats.StartingVelocity != null)
                    particleStats.StartingVelocity = stats.StartingVelocity.Length == 1 ? stats.StartingVelocity[0] : RandomHelper.Instance.GetVector2(stats.StartingVelocity[0], stats.StartingVelocity[1]);
                particleStats.ActingForce = stats.ActingForce;
                particleStats.Font = stats.Font;
                particleStats.ParticleText = stats.ParticleText;
                if (stats.ParticleRotation != null)
                    particleStats.Rotation = stats.ParticleRotation.Length == 1 ? stats.ParticleRotation[0] : RandomHelper.Instance.GetFloat(stats.ParticleRotation[0], stats.ParticleRotation[1]);
                if (stats.ParticleSpeed != null)
                    particleStats.Speed = stats.ParticleSpeed.Length == 1 ? stats.ParticleSpeed[0] : RandomHelper.Instance.GetFloat(stats.ParticleSpeed[0], stats.ParticleSpeed[1]);

                particleStats.ColorDecayRate = stats.ParticleColorDecayRate;
                particleStats.SizeDecayRate = stats.ParticleSizeDecayRate;

                particleStats.Lifespan = stats.ParticleLifeSpan.Length == 1 ? stats.ParticleLifeSpan[0] : RandomHelper.Instance.GetFloat(stats.ParticleLifeSpan[0], stats.ParticleLifeSpan[1]);

                if (stats.TrackLayerDepth != null)
                    particleStats.LayerDepth = stats.TrackLayerDepth.Invoke();

                Particle particle = new Particle(particleStats);
                particles.Add(particle);
            }
        }

        private void CullParticles()
        {
            for (int i = 0; i < particleIndices.Count; i++)
            {
                particles[particleIndices[i]].Reset();
            }

            particleIndices.Clear();
        }

        public void SwapTrackPosition(GetVector position)
        {
            stats.TrackPosition = position;
        }

        public void SetCurrentSpawnBounds(int ind) => stats.CurrentBounds = ind;
        public void SetStartingVelocity(Vector2[] vectors)
        {
            stats.StartingVelocity = vectors;
            if (vectors.Length > 0)
                SetParticlesStartingVelocity(RandomHelper.Instance.GetVector2(vectors[0], vectors[1]));
            else
                SetParticlesStartingVelocity(vectors[0]);
        }
        public void SetParticlesVelocity(Vector2 vector)
        {
            foreach (Particle particle in particles)
            {
                particle.SetVelocity(vector);
            }
        }

        public void SetParticlesStartingVelocity(Vector2 vector)
        {
            foreach (Particle particle in particles)
            {
                particle.SetStartingVelocity(vector);
            }
        }

        public Rectangle[] GetCurrentSpawnBounds() => bounds[stats.CurrentBounds];

        /// <summary>
        /// Resets particle list
        /// </summary>
        public void Reset()
        {
            particles.Clear();
        }
    }
}
