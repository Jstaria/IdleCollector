using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public class AudioController: IUpdatable
    {
        private static AudioController instance;
        public static AudioController Instance
        {
            get
            {
                if (instance == null)
                    instance = new AudioController();
                return instance;
            }
        }

        public AudioController() { Initialize(); }

        private Dictionary<string, Song> album;
        private Dictionary<string, SoundEffect> soundEffects;
        private List<Song> queue;
        private Song playingSong;

        private float musicVolume;
        private float soundEffectVolume;
        private float masterVolume;
        private int queueIndex;

        private List<SoundEffectInstance> soundEffectInstances;

        public bool LoopMusic { get; set; }

        public void Initialize()
        {
            soundEffects = ResourceAtlas.GetSoundEffects();
            soundEffectInstances = new();

            album = ResourceAtlas.GetSongs();
            musicVolume = VolumeController.Instance.MusicVolume;
            soundEffectVolume = VolumeController.Instance.SoundEffectVolume;
            masterVolume = VolumeController.Instance.MasterVolume;

            VolumeController.Instance.MusicVolumeEvent += ChangeMusic;
            VolumeController.Instance.MasterVolumeEvent += ChangeMaster;
            VolumeController.Instance.SoundEffectVolumeEvent += ChangeSoundEffect;

            MakeQueue();

            playingSong = queue[0];
            MediaPlayer.Play(playingSong);
            MediaPlayer.Volume = musicVolume * masterVolume;
        }

        public void ChangeMusic(float volume) => musicVolume = volume; 
        public void ChangeMaster(float volume) => masterVolume = volume;
        public void ChangeSoundEffect(float volume) => soundEffectVolume = volume;

        public void ControlledUpdate(GameTime gameTime)
        {
            
        }

        public void StandardUpdate(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.OemOpenBrackets)) VolumeController.Instance.ChangeVolume("MasterVolume", VolumeController.Instance.MasterVolume - .1f);
            if (Input.IsButtonDownOnce(Keys.OemCloseBrackets)) VolumeController.Instance.ChangeVolume("MasterVolume", VolumeController.Instance.MasterVolume + .1f);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            PlayNextSong();

            MediaPlayer.Volume = musicVolume * masterVolume;

            CleanSoundInstances();
        }

        #region Music
        private void PlayNextSong()
        {
            if (MediaPlayer.State == MediaState.Playing) return;

            int index = (queueIndex + 1);

            if (index > queue.Count && !LoopMusic) return;

            queueIndex = index % queue.Count;
            playingSong = queue[queueIndex];

            MediaPlayer.Play(playingSong);
        }

        private void MakeQueue()
        {
            Song lastsong = album.ElementAt(RandomHelper.Instance.GetIntExclusive(0, album.Count)).Value;
            Song currentSong = null;

            queue = new();

            for (int i = 0; i < 20; i++)
            {
                do
                {
                    currentSong = album.ElementAt(RandomHelper.Instance.GetIntExclusive(0, album.Count)).Value;
                }
                while (lastsong == currentSong);

                queue.Add(currentSong);
                lastsong = currentSong;
            }
        }
        #endregion
        #region Sound Effect
        public void PlaySoundEffect(string name, float pitch)
        {
            SoundEffectInstance effectInstance = soundEffects[name].CreateInstance();

            effectInstance.Volume = masterVolume * soundEffectVolume;
            effectInstance.Pitch = pitch;
            
            effectInstance.Play();

            soundEffectInstances.Add(effectInstance);
        }

        private void CleanSoundInstances()
        {
            soundEffectInstances = soundEffectInstances.Where((w) => (w.State == SoundState.Playing)).ToList();
        }
        #endregion
    }
}
