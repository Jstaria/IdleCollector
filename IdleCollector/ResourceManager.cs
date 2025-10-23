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
using System.Diagnostics;

namespace IdleCollector
{
    public class ResourceInfo
    {
        public int Count;
        public int Multiplier;
        public string Name;
        public bool IsUnlocked;
        public bool HasTrail;
        public string IconTextureKey;
        public Color ResourceColor;
        
        [JsonIgnore]
        public TrailInfo trailInfo;

        public ResourceInfo(string name)
        {
            Count = 0;
            Name = name;
            IsUnlocked = false;
            IconTextureKey = name + "Icon";
            Multiplier = 1;
        }

        public void Reset()
        {
            Count = 0;
            IsUnlocked = false;
        }
    }

    public class ResourceManager : ISaveable, IRenderable, IUpdatable, ITransform
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
        [JsonIgnore]
        public Vector2 Position { get; set; }

        private string resourceUIKey = "resourceBar";
        private string UIFontKey = "DePixelHalbfett";

        [JsonProperty]
        private Dictionary<string, ResourceInfo> resources;
        private List<ResourceUIObject> resourceObjs;

        private string jsonPath = "Content/SaveData/ResourceData.json";

        public ResourceManager()
        {
            instance = this;

            Initialize();
        }

        private void Initialize()
        {
            LayerDepth = .75f;
            Color = Color.White;
            resourceObjs = new();

            Load();
            LoadTrailInfo();
        }

        public void AddPointsTo(string name, int count)
        {
            ResourceInfo accessedResource = resources[name];
            accessedResource.Count += count * accessedResource.Multiplier;
        }

        private void DespawnResourceUIObj(ResourceUIObject obj)
        {
            AddPointsTo(obj.ResourceInfo.Name, obj.ResourceInfo.Count);
            resourceObjs.Remove(obj);

            AudioController.Instance.PlaySoundEffect("resourceCollect2", "soundEffectVolume",RandomHelper.Instance.GetFloat(0,1));
        }

        public void SpawnResourceUIObj(Vector2 worldPosition, ResourceInfo info)
        {
            ResourceUIObject obj = new ResourceUIObject(
                .75f,
                info, 
                Renderer.GetScreenPosition(worldPosition), 
                new Vector2(50, 1030), 
                LayerDepth, 
                DespawnResourceUIObj,
                RandomHelper.Instance.GetFloat(.75f, 1.25f));

            if (resources[info.Name].HasTrail)
            {
                TrailInfo trailInfo = resources[info.Name].trailInfo;
                trailInfo.SegmentColor = (t) => Color.Lerp(Color.White, resources[info.Name].ResourceColor, MathF.Pow(obj.T, .25f));

                obj.CreateTrail(trailInfo);
            }

            resourceObjs.Add(obj);
        }

        public void LoadTrailInfo()
        {
            TrailInfo info = new TrailInfo();
            info.SegmentColor = (t) => Color.White;
            info.TrackLayerDepth = () => LayerDepth;
            info.TrackPosition = () => new Vector2(1000, 0);
            info.NumberOfSegments = 100;
            info.TrailLength = 250;
            info.TipWidth = 8;
            info.EndWidth = 0;
            info.SegmentsPerSecond = (float)info.NumberOfSegments * .5f;
            info.SegmentsRemovedPerSecond = info.SegmentsPerSecond * .5f;
            info.HasOutline = true;
            info.OutlineThickness = 4;
            info.OutlineColor = Color.White * .25f;

            resources["Grass"].trailInfo = info;

            info = new TrailInfo();
            info.SegmentColor = (t) => Color.White;
            info.TrackLayerDepth = () => LayerDepth;
            info.TrackPosition = () => new Vector2(1000, 0);
            info.NumberOfSegments = 100;
            info.TrailLength = 250;
            info.TipWidth = 12;
            info.EndWidth = 0;
            info.SegmentsPerSecond = (float)info.NumberOfSegments * .5f;
            info.SegmentsRemovedPerSecond = info.SegmentsPerSecond * .5f;
            info.HasOutline = true;
            info.OutlineThickness = 8;
            info.OutlineColor = Color.White * .25f;

            resources["Flower"].trailInfo = info;
        }

        public void Load()
        {
            FileIO.ReadJsonInto(this, jsonPath);
        }

        public void Save()
        {
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

                float depth = LayerDepth - i * .005f;

                sb.Draw(tex, Position, null, Color, 0, offset, 1, SpriteEffects.None, depth);
                sb.Draw(icon, Position, null, resources[i].ResourceColor, 0, offset, 1, SpriteEffects.None, depth + .001f);
                sb.DrawString(font, text, Position, resources[i].ResourceColor, 0, offset - new Vector2(tex.Width - textDim.X - 8, textDim.Y / 2), 1, SpriteEffects.None, depth + .001f);
            }

            for (int i = 0; i < resourceObjs.Count; i++)
            {
                resourceObjs[i].Draw(sb);
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

        public void ControlledUpdate(GameTime gameTime)
        {
            for (int i = 0; i < resourceObjs.Count; i++)
            {
                resourceObjs[i].Update(gameTime);
            }
        }

        public void StandardUpdate(GameTime gameTime)
        {

        }

        public void SlowUpdate(GameTime gameTime)
        {

        }
    }
}
