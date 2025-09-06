using IdleCollector;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public static class ResourceAtlas
    {

        // Texture2D Resource
        private static Texture2D tilemapAtlas;
        private static Point tileSize;
        private static Dictionary<string, Point> tilemapAtlasKeys;
    
        // Sound Effects and Audio
        private static Dictionary<string, SoundEffect> soundEffects;
        private static Dictionary<string, Song> songs;

        public static Texture2D TilemapAtlas { get => tilemapAtlas; }
        public static Rectangle GetTileRect(string tileName)
        {
            if (TilemapAtlas == null) throw new Exception("Tilemap Atlas is empty!");
            if (tileSize == Point.Zero) throw new Exception("Tile size not set!");
            if (tilemapAtlasKeys == null) throw new Exception("Tilemap atlas keys missing!");
            if (!tilemapAtlasKeys.ContainsKey(tileName)) throw new Exception(String.Format("Tile {0} does not exist!", tileName));
            
            return new Rectangle(tilemapAtlasKeys[tileName], tileSize);
        }

        public static void LoadTilemap(ContentManager Content, string tilemapPath, string tilemapKeysPath, Point tilemapResolution)
        {
            tilemapAtlasKeys = new Dictionary<string, Point>();
            tilemapAtlas = Content.Load<Texture2D>(tilemapPath);

            List<string> fileLines = FileIO.ReadFrom(tilemapKeysPath);

            tileSize = new Point(tilemapAtlas.Width / tilemapResolution.X, tilemapAtlas.Height / tilemapResolution.Y);

            for (int j = 0; j < fileLines.Count; j++)
            {
                string[] names = fileLines[j].Split(',');

                for (int i = 0; i < names.Length; i++)
                {
                    Point position = new Point(tileSize.X * i, tileSize.Y * j);
                    tilemapAtlasKeys.Add(names[i], position);        
                }
            }

        }

        /// <summary>
        /// Load songs
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="fullFilePath">Full directory file path</param>
        /// <param name="folder">Folder path</param>
        /// <param name="fileType">Ending suffix only (Ex. mp3)</param>
        public static void LoadSongs(ContentManager Content, string fullFilePath, string folder, string fileType)
        {
            songs = new Dictionary<string, Song>();

            DirectoryInfo di = new DirectoryInfo(fullFilePath);
            FileInfo[] files = di.GetFiles("*." + fileType);

            int filesLength = files.Length;

            for (int i = 0; i < filesLength; i++)
            {
                string name = files[i].Name.Remove(files[i].Name.Length - 4, 4);
                Song media = Content.Load<Song>(folder + "/" + name);

                songs.Add(name, media);
            }
        }

        /// <summary>
        /// Load songs
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="fullFilePath">Full directory file path</param>
        /// <param name="folder">Folder path</param>
        /// <param name="fileType">Ending suffix only (Ex. mp3)</param>
        public static void LoadSoundEffects(ContentManager Content, string fullFilePath, string folder, string fileType)
        {
            soundEffects = new Dictionary<string, SoundEffect>();

            DirectoryInfo di = new DirectoryInfo(fullFilePath);
            FileInfo[] files = di.GetFiles("*." + fileType);

            int filesLength = files.Length;

            for (int i = 0; i < filesLength; i++)
            {
                string name = files[i].Name.Remove(files[i].Name.Length - 4, 4);
                SoundEffect media = Content.Load<SoundEffect>(folder + "/" + name);

                soundEffects.Add(name, media);
            }
        }
    }
}
