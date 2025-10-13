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
    public class MusicController: IUpdatable
    {
        private static MusicController instance;
        public static MusicController Instance
        {
            get
            {
                if (instance == null)
                    instance = new MusicController();
                return instance;
            }
        }

        public MusicController() { Initialize(); }

        private Dictionary<string, Song> album;
        private List<Song> queue;
        private Song playingSong;

        private float volume;
        private float masterVolume;
        private int queueIndex;

        public bool Loop { get; set; }

        public void Initialize()
        {
            album = ResourceAtlas.GetSongs();
            volume = VolumeController.Instance.MusicVolume;
            masterVolume = VolumeController.Instance.MasterVolume;

            VolumeController.Instance.MusicVolumeEvent += ChangeVolume;
            VolumeController.Instance.MasterVolumeEvent += ChangeMaster;

            MakeQueue();

            playingSong = queue[0];
            MediaPlayer.Play(playingSong);
            MediaPlayer.Volume = volume * masterVolume;
        }

        public void ChangeVolume(float volume) => this.volume = volume; 
        public void ChangeMaster(float volume) => masterVolume = volume;

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

            MediaPlayer.Volume = volume * masterVolume;
        }

        private void PlayNextSong()
        {
            if (MediaPlayer.State == MediaState.Playing) return;

            int index = (queueIndex + 1);

            if (index > queue.Count && !Loop) return;

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
    }
}
