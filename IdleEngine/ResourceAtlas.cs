using IdleCollector;
using Microsoft.VisualBasic.FileIO;
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
        private static Dictionary<string, Texture2D> textureCache;
        private static Dictionary<string, Dictionary<string, Rectangle>> tilemapAtlasKeys;
    
        // Sound Effects and Audio
        private static Dictionary<string, SoundEffect> soundEffects;
        private static Dictionary<string, Song> songs;

        // Fonts
        private static Dictionary<string, SpriteFont> fonts;

        public static Dictionary<string, Dictionary<string, Rectangle>> TilemapAtlasKeys { get => tilemapAtlasKeys; }
        public static Texture2D TilemapAtlas { get => tilemapAtlas; }
        public static Rectangle GetTileRect(string accessKey, string tileName)
        {
            if (TilemapAtlas == null) throw new Exception("Tilemap Atlas is empty!");
            if (tilemapAtlasKeys == null) throw new Exception("Tilemap atlas keys missing!");
            if (tilemapAtlasKeys[accessKey] == null) throw new Exception(String.Format("Tilemap atlas does not contain access key {0}", accessKey));
            if (!tilemapAtlasKeys[accessKey].ContainsKey(tileName)) throw new Exception(String.Format("Tile {0} does not exist!", tileName));
            
            return tilemapAtlasKeys[accessKey][tileName];
        }
        public static Rectangle GetRandomTileRect(string accessKey) => GetTileRect(accessKey, GetRandomAtlasKey(accessKey));
        public static string GetRandomAtlasKey(string accessKey)
        {
            List<string> keys = tilemapAtlasKeys[accessKey].Keys.ToList();

            string tileName = keys[RandomHelper.Instance.GetIntExclusive(0, keys.Count)];
            return tileName;
        }
        public static Texture2D GetTexture(string name)
        {
            if (!textureCache.ContainsKey(name)) throw new Exception(string.Format("Resource atlas does not contain texture!: {0}", name));

            return textureCache[name];
        }

        public static SpriteFont GetFont(string name)
        {
            if (!fonts.ContainsKey(name)) throw new Exception(string.Format("Font: {0} not found in resource atlas!", name));

            return fonts[name];
        }

        public static void LoadTilemap(ContentManager Content, string tilemapKeysPath, string tilemapPath)
        {
            tilemapAtlasKeys = new Dictionary<string, Dictionary<string, Rectangle>>();
            tilemapAtlas = Content.Load<Texture2D>(tilemapPath);

            List<string> fileLines = FileIO.ReadFrom(tilemapKeysPath);

            int xOffset = 0;
            int yOffset = 0;

            for (int j = 0; j < fileLines.Count; j++)
            {
                string[] entries = fileLines[j].Split(' ');

                string[] dim = entries[0].Split('x');

                int x = int.Parse(dim[0]);  
                int y = int.Parse(dim[1]);

                for (int k = 1; k < entries.Length; k++)
                {
                    string[] title = entries[k].Split(':');
                    string[] names = title[1].Split(",");

                    tilemapAtlasKeys.Add(title[0], new());

                    for (int i = 0; i < names.Length; i++)
                    {
                        Point position = new Point(xOffset, yOffset);
                        tilemapAtlasKeys[title[0]].Add(names[i], new Rectangle(position, new Point(x,y)));
                    
                        xOffset += x;
                    }
                }
                
                xOffset = 0;
                yOffset += y;
            }

        }

        public static void LoadTextures(ContentManager Content, string fullFilePath, string folder)
        {
            textureCache = new Dictionary<string, Texture2D>();

            DirectoryInfo di = new DirectoryInfo(fullFilePath);
            FileInfo[] files = di.GetFiles("*.png");

            int filesLength = files.Length;

            for (int i = 0; i < filesLength; i++)
            {
                string name = files[i].Name.Remove(files[i].Name.Length - 4, 4);
                Texture2D media = Content.Load<Texture2D>(folder + "/" + name);

                textureCache.Add(name, media);
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

        public static void LoadFonts(ContentManager Content, string fullFilePath, string folder)
        {
            fonts = new Dictionary<string, SpriteFont>();

            DirectoryInfo di = new DirectoryInfo(fullFilePath);
            FileInfo[] files = di.GetFiles("*.spritefont");

            int filesLength = files.Length;

            for (int i = 0; i < filesLength; i++)
            {
                string name = files[i].Name.Remove(files[i].Name.Length - 11, 11);
                SpriteFont media = Content.Load<SpriteFont>(folder + "/" + name);

                fonts.Add(name, media);
            }
        }
    }
}
