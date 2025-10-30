using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace IdleCollector
{
    #region // Extra Classes
    internal class UIObj
    {
        public Vector2 offset;
        public Spring xSpring;
        public Texture2D backing;
        public Texture2D icon;
        public SpriteFont font;
        public string text;
        public float layerDepth;
        public Color color;

        public UIObj(string textureBack, string icon, string font, Color color, Vector2 offset, float layerDepth = .85f)
        {
            xSpring = new Spring(/*AngularFrequency*/20, /*DampingRatio*/1, 0);
            backing = ResourceAtlas.GetTexture(textureBack);
            this.icon = ResourceAtlas.GetTexture(icon);
            this.font = ResourceAtlas.GetFont(font);
            this.color = color;
            this.offset = offset;
            this.layerDepth = layerDepth;
        }

        public void SetText(string text) => this.text = text;

        public void Update() => xSpring.Update();
        public void Nudge(float x) => xSpring.Nudge(x);

        public void Draw(SpriteBatch sb, Vector2 position)
        {
            Vector2 textDim = font.MeasureString(text);

            Vector2 Position = position + Vector2.UnitX * xSpring.Position;

            sb.Draw(backing, Position, null, Color.White, 0, offset, 1, SpriteEffects.None, layerDepth);
            sb.Draw(icon, Position, null, color, 0, offset, 1, SpriteEffects.None, layerDepth + .001f);
            sb.DrawString(font, text, Position, color, 0, offset - new Vector2(backing.Width - textDim.X - 8, textDim.Y / 2), 1, SpriteEffects.None, layerDepth + .001f);
        }
    }
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
    #endregion

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
        
        [JsonIgnore]
        private ParticleSystem resourceCollectParticles;
        [JsonIgnore]
        private Vector2 particleSpawnPos;
        private Color particleColor;

        private string resourceUIKey = "resourceBar";
        private string UIFontKey = "DePixelHalbfett";

        [JsonProperty]
        private Dictionary<string, ResourceInfo> resources;
        private List<ResourceUIObject> resourceObjs;
        private Dictionary<string, UIObj> uiObjs;

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
            LoadUIObjs();
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

            uiObjs[obj.ResourceInfo.Name].Nudge(-150);

            if (resources[obj.ResourceInfo.Name].HasTrail)
                AudioController.Instance.PlaySoundEffect("resourceCollect2", "soundEffectVolume", RandomHelper.Instance.GetFloat(0, 1));
        }

        public void SpawnResourceUIObj(Vector2 worldPosition, ResourceInfo info)
        {
            ResourceUIObject obj = new ResourceUIObject(
                .75f,
                info,
                Renderer.GetScreenPosition(worldPosition),
                Position - uiObjs[info.Name].offset + 32 * Vector2.One,
                LayerDepth,
                DespawnResourceUIObj,
                RandomHelper.Instance.GetFloat(.75f, 1.25f));

            if (resources[info.Name].HasTrail)
            {
                particleSpawnPos = Renderer.GetScreenPosition(worldPosition);
                particleColor = Color.White;//resources[info.Name].ResourceColor;
                ParticleSystemStats stats = resourceCollectParticles.Stats;
                stats.ParticleStartColor = new Color[] { particleColor };
                resourceCollectParticles.Stats = stats;
                resourceCollectParticles.EmitParticles();

                TrailInfo trailInfo = resources[info.Name].trailInfo;
                trailInfo.SegmentColor = (t) => Color.Lerp(Color.White, resources[info.Name].ResourceColor, MathF.Pow(obj.T, 2));

                obj.CreateTrail(trailInfo);
            }

            resourceObjs.Add(obj);
        }
        #region // Load
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
            info.TipWidth = 6;
            info.EndWidth = 0;
            info.SegmentsPerSecond = (float)info.NumberOfSegments * .5f;
            info.SegmentsRemovedPerSecond = info.SegmentsPerSecond * .5f;
            info.HasOutline = true;
            info.OutlineThickness = 4;
            info.OutlineColor = Color.White * .51f;

            resources["Flower"].trailInfo = info;
        }
        public void LoadUIObjs()
        {
            ParticleSystemStats stats = new ParticleSystemStats();
            stats.ParticleSize = new float[] { 2 };
            stats.ParticleSpeed = new float[] { .5f, 4f };
            stats.EmitRate = new float[] { 0 };
            stats.EmitCount = new int[] { 2, 5 };
            stats.ParticleStartColor = new Color[] { Color.White };
            stats.ParticleEndColor = new Color[] { Color.Transparent };
            stats.MaxParticleCount = 300;
            stats.ParticleColorDecayRate += (float t) => t;
            stats.ParticleSizeDecayRate += (float t) => 1 - t;
            stats.ParticleDespawnDistance = 500;
            stats.TrackLayerDepth = () => .95f;
            stats.TrackPosition = () => particleSpawnPos;
            stats.ParticleTextureKeys = new string[] { "sparkle1", "sparkle2", "sparkle3" };

            Rectangle[][] bounds = new Rectangle[][]
            {
                new Rectangle[] { new Rectangle(0, 0, 1, 1)},
            };

            stats.SpawnBounds = bounds;
            stats.UseRandomBounds = false;
            stats.ParticleRotation = new float[] { 0 };
            stats.ParticleRotationSpeed = (t) => 0;
            stats.ParticleLifeSpan = new float[] { .5f, .75f };
            stats.ResetParticlesAfterDeath = false;

            stats.StartingVelocity = new Vector2[] { Vector2.One, -Vector2.One };

            resourceCollectParticles = new ParticleSystem(stats);

            uiObjs = new();
            for (int i = 0; i < resources.Values.Count; i++)
            {
                ResourceInfo info = resources.Values.ToList()[i];
                Texture2D tex = ResourceAtlas.GetTexture(resourceUIKey);
                Vector2 offset = new Vector2(0, (i * tex.Height) + (i * -4) + tex.Height);
                UIObj obj = new UIObj(resourceUIKey, info.IconTextureKey, UIFontKey, info.ResourceColor, offset);
                uiObjs.Add(info.Name, obj);
            }
        }
        #endregion
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
            List<UIObj> objs = this.uiObjs
                .Where(w => resources.Contains(this.resources[w.Key]))
                .Select(w => w.Value)
                .ToList();

            for (int i = 0; i < uiObjs.Count; i++)
            {
                string text = resources[i].Count.ToString();

                objs[i].SetText(text);
                objs[i].Draw(sb, Position);
            }

            for (int i = 0; i < resourceObjs.Count; i++)
            {
                resourceObjs[i].Draw(sb);
            }

            resourceCollectParticles.Draw(sb);
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
            resourceCollectParticles.ControlledUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (UIObj obj in uiObjs.Values)
            {
                obj.Update();
            }

            resourceCollectParticles.StandardUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            resourceCollectParticles.SlowUpdate(gameTime);
        }
    }
}
