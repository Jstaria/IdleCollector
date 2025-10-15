using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class AmbienceController: IUpdatable
    {
        private List<string> continuousAmbience;
        private List<string> randomAmbience;

        private Dictionary<string, SoundEffect> sounds;
        private Dictionary<string, SoundEffectInstance> continuousAmbiences;

        private static AmbienceController instance;
        public static AmbienceController Instance
        {
            get
            {
                if (instance == null)
                    instance = new AmbienceController();
                return instance;
            }
        }

        public AmbienceController()
        {
            continuousAmbience = new List<string>();
            continuousAmbiences = new();
            randomAmbience = new List<string>();

            sounds = ResourceAtlas.GetSoundEffects();
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            
        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }

        public void StandardUpdate(GameTime gameTime)
        {
            
        }

        public void AddContAmbience(params string[] cont) => continuousAmbience.AddRange(cont);
        public void AddRandAmbience(params string[] cont) => randomAmbience.AddRange(cont);

        public void KillAmbience()
        {
            if (continuousAmbiences == null) return;

            foreach (string cont in continuousAmbiences.Keys)
            {
                continuousAmbiences[cont].Stop();
            }
        }

        public void PlayContAmbience()
        {
            KillAmbience();
            
            continuousAmbiences.Clear();

            foreach (string cont in continuousAmbience)
            {
                SoundEffectInstance instance = sounds[cont].CreateInstance();
                instance.IsLooped = true;
                instance.Play();

                continuousAmbiences.Add(cont, instance);
            }
        }
    }
}
