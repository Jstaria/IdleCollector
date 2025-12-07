using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class VolumeController
    {
        public delegate void OnVolumeChange(float volume);

        private static VolumeController instance;
        public static VolumeController Instance
        {
            get
            {
                if (instance == null)
                    instance = new VolumeController();
                return instance;
            }
        }
        public VolumeController() { Initialize(); }

        public float MasterVolume { get; set; }

        public event OnVolumeChange MasterVolumeEvent;
        public float SoundEffectVolume { get; set; }
        public event OnVolumeChange SoundEffectVolumeEvent;
        public float MusicVolume { get; set; }
        public event OnVolumeChange MusicVolumeEvent;
        public float CharacterVolume { get; set; }
        public event OnVolumeChange CharacterVolumeEvent;
        public float AmbientVolume { get; set; }
        public event OnVolumeChange AmbientVolumeEvent;

        public void Initialize()
        {
            FileIO.ReadJsonInto(this, "Content/SaveData/VolumeData");
        }

        public void Save()
        {
            FileIO.WriteJsonTo(this, "Content/SaveData/VolumeData", Newtonsoft.Json.Formatting.Indented);
        }

        public void ChangeVolume(string volumeName, float volume)
        {
            Type type = typeof(VolumeController);
            PropertyInfo property = type.GetProperty(volumeName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            FieldInfo field = type.GetField(volumeName + "Event", BindingFlags.Instance | BindingFlags.NonPublic);
            MulticastDelegate del = field?.GetValue(this) as MulticastDelegate;
            
            volume = MathHelper.Clamp(volume, 0, 1);
            
            del?.DynamicInvoke(volume);
            property.SetValue(this, volume);

            Save();
        }

        public void IncrementVolume(string volumeName, float amt)
        {
            Type type = typeof(VolumeController);
            PropertyInfo property = type.GetProperty(volumeName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            FieldInfo field = type.GetField(volumeName + "Event", BindingFlags.Instance | BindingFlags.NonPublic);
            MulticastDelegate del = field?.GetValue(this) as MulticastDelegate;

            float volumeLevel = (float)property.GetValue(this);
            float volume = MathHelper.Clamp(volumeLevel + amt, 0, 1);

            del?.DynamicInvoke(volume);
            property.SetValue(this, volume);

            Save();
        }
    }
}
