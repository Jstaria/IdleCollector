using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using IdleEngine;
using Microsoft.Xna.Framework.Graphics;

namespace IdleCollector
{
    internal class ResourceInfo
    {
        public int Count;
        public string Name;
        public bool IsUnlocked;
        public string IconTextureKey;
        public Color ResourceColor;

        public ResourceInfo(string name)
        {
            Count = 0;
            Name = name;
            IsUnlocked = false;
            IconTextureKey = name + "Icon";
        }

        public void Reset()
        {
            Count = 0;
            IsUnlocked = false;
        }
    }
    internal class Resource
    {
        public string Name;
    }
    internal class ResourceManager : ISaveable, IRenderable, ITransform
    {
        private static ResourceManager instance;
        public static ResourceManager Instance
        {
            get
            {
                if (instance == null) instance = new();
                
                return instance;
            }
        }
        [JsonIgnore]
        public float LayerDepth { get; set; }
        [JsonIgnore]
        public Color Color { get; set; }
        public Vector2 Position { get; set; }

        private string resourceUIKey = "resourceBar";
        private string UIFontKey = "DePixelHalbfett";

        [JsonProperty]
        private Dictionary<string, ResourceInfo> resources;

        private string jsonPath = "../../../Content/SaveData/ResourceData.json";

        public ResourceManager()
        {
            instance = this;

            Initialize();
        }

        private void Initialize()
        {
            LayerDepth = .75f;
            Color = Color.White;

            Load();
        }

        public void Load()
        {
            FileIO.ReadJsonInto(this, jsonPath);
        }

        public void Save()
        {
            resources["cactus"].Count++;
            FileIO.WriteJsonTo(this, jsonPath, Formatting.Indented);
        }

        public void Reset()
        {
            foreach (ResourceInfo resource in resources.Values)
            {
                resource.Reset();
            }

            Save();
        }

        public void Draw(SpriteBatch sb)
        {
            List<ResourceInfo> resources = this.resources.Values.Where(w => w.IsUnlocked).ToList();

            for (int i = 0; i < resources.Count; i++)
            {
                string text = resources[i].Count.ToString();

                Texture2D tex = ResourceAtlas.GetTexture(resourceUIKey);
                Texture2D icon = ResourceAtlas.GetTexture(resources[i].IconTextureKey);
                SpriteFont font = ResourceAtlas.GetFont(UIFontKey);
                
                Vector2 offset = new Vector2(0, (i * tex.Height) + (i * -4) + tex.Height);
                Vector2 textDim = font.MeasureString(text);

                sb.Draw(tex, Position, null, Color, 0, offset, 1, SpriteEffects.None, LayerDepth);
                sb.Draw(icon, Position, null, resources[i].ResourceColor, 0, offset, 1, SpriteEffects.None, LayerDepth);
                sb.DrawString(font, text, Position, resources[i].ResourceColor, 0, offset - new Vector2(tex.Width - textDim.X - 8, textDim.Y / 2), 1, SpriteEffects.None, LayerDepth);
            }
        }

        public void Move(Vector2 direction)
        {
            Position += direction;
        }

        public void MoveTo(Vector2 position)
        {
            Position = position;
        }
    }
}
