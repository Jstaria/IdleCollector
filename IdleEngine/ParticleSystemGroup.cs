using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace IdleEngine
{
    public class ParticleSystemGroup : IRenderable, IUpdatable
    {
        private readonly List<ParticleSystem> systems = new();

        public IReadOnlyList<ParticleSystem> Systems => systems;
        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public ParticleSystem Add(ParticleSystemStats stats)
        {
            ParticleSystem system = new ParticleSystem(stats);
            systems.Add(system);
            return system;
        }

        public ParticleSystem Add(ParticleSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            systems.Add(system);
            return system;
        }

        public bool Remove(ParticleSystem system) => systems.Remove(system);

        public void EmitParticles()
        {
            foreach (ParticleSystem system in systems)
                system.EmitParticles();
        }

        public void EmitParticles(int systemIndex)
        {
            systems[systemIndex].EmitParticles();
        }

        public void Reset()
        {
            foreach (ParticleSystem system in systems)
                system.Reset();
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            foreach (ParticleSystem system in systems)
                system.ControlledUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (ParticleSystem system in systems)
                system.StandardUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            foreach (ParticleSystem system in systems)
                system.SlowUpdate(gameTime);
        }

        public void Draw(SpriteBatch sb)
        {
            foreach (ParticleSystem system in systems)
                system.Draw(sb);
        }
    }
}
