using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTree : IScene
    {
        public static SkillTree Instance => instance ??= new SkillTree();
        private static SkillTree instance;

        private const float LinkLineThickness = 30f;
        private const float LinkPulseTravelTime = 3f;
        private const float HoverAuraRadius = 32f;

        [JsonRequired] public int GridSpacing = 100;
        [JsonRequired] public int IconSize = 50;
        [JsonIgnore] public Point IconSizePoint;
        [JsonRequired] public string DefaultIcon = "default-icon";
        [JsonRequired] public int TokenID = 0;
        [JsonIgnore] public Vector2 iconOrigin;
        [JsonIgnore] public Texture2D iconTexture;

        [JsonProperty] private Dictionary<int, SkillTreeToken> treeTokens;
        [JsonIgnore] private Dictionary<Point, SkillTreeToken> tokenPositions;
        [JsonIgnore] private readonly Dictionary<(int Parent, int Child), LinkParticleSystem> linkParticles = new();
        [JsonIgnore] public LinkParticleSettings LinkParticleSettings { get; } = new();
        [JsonIgnore] internal SkillTreeShockwave Shockwave { get; set; }
        [JsonIgnore] private ParticleSystem unlockBurstParticles;
        [JsonIgnore] private Vector2 unlockBurstPosition;

        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public Color Color { get; set; }


        private Point mouseStart;
        private Point cameraStart;
        private Point lastCameraPosition;

        private BasicEffectRenderable _renderable;
        private LinkPulseRenderable linkPulseRenderable;
        public string SkillTreeScene = "SkillTreeScene";
        [JsonIgnore] private float time;
        [JsonIgnore] private int hoverAuraTokenId = -1;
        [JsonIgnore] private float hoverAuraAlpha;
        [JsonIgnore] private Vector4 hoverAuraTimeOffsets;
        [JsonIgnore] public float zoom = 1;
        [JsonIgnore] public float zoomTarget = 1;
        [JsonIgnore] public bool editing = false;


        public SkillTree()
        {
            treeTokens = new();

            LoadSkillTree();

            IconSizePoint = new Point(IconSize);
            iconTexture = ResourceAtlas.GetTexture(DefaultIcon);
            iconOrigin = new Vector2(iconTexture.Width / 2, iconTexture.Height / 2);

            SetFamilyTokens();

            _renderable = new BasicEffectRenderable(ResourceAtlas.GetEffect("Stars"), new Vector2(500, 300), Vector2.Zero);

            _renderable.DrawEvent += (effect) =>
            {
                _renderable.Position = -Renderer.CurrentCamera.Position.ToVector2() - Vector2.One * 10;

                effect.Parameters["iTime"].SetValue((float)time);
                effect.Parameters["iPosition"].SetValue(Renderer.CurrentCamera.Position.ToVector2() / new Vector2(480, 480));
            };

            Updater.AddToSceneExit(SkillTreeScene, () => {
                Renderer.CurrentCamera.Zoom = 1;
            });

            Renderer.AddToSceneEarlyDraw(SkillTreeScene, _renderable);

            linkPulseRenderable = new LinkPulseRenderable(
                ResourceAtlas.GetEffect("LinkPulse"),
                DrawLinkPulseLines,
                () => time);
            Renderer.AddToSceneEarlyDraw(SkillTreeScene, linkPulseRenderable);

            unlockBurstParticles = CreateUnlockBurstParticles();
            instance = this;
        }

        private void SetFamilyTokens()
        {
            foreach (SkillTreeToken token in treeTokens.Values)
            {
                token.ChildTokens.Clear();
                token.ParentTokens.Clear();
                EnsureIconLayers(token);
                token.ParentTokenIDs ??= new List<int>();
                token.ChildTokenIDs ??= new List<int>();
                token.ParentTokenIDs = token.ParentTokenIDs
                    .Where(parentId => parentId != token.TokenID && treeTokens.ContainsKey(parentId))
                    .Distinct()
                    .ToList();
                token.ChildTokenIDs.Clear();
            }

            foreach (SkillTreeToken child in treeTokens.Values)
            {
                foreach (int parentId in child.ParentTokenIDs)
                {
                    SkillTreeToken parent = treeTokens[parentId];
                    parent.ChildTokenIDs.Add(child.TokenID);
                    parent.ChildTokens.Add(child);
                    child.ParentTokens.Add(parent);
                }
            }

            RefreshTokenDepths();
        }

        private void EnsureIconLayers(SkillTreeToken token)
        {
            token.IconLayers ??= new List<IconLayer>();
            if (token.IconLayers.Count == 0)
            {
                token.IconLayers.Add(new IconLayer
                {
                    Icon = DefaultIcon,
                    Width = IconSize,
                    Height = IconSize
                });
            }

            foreach (IconLayer layer in token.IconLayers)
            {
                if ((layer.Width <= 0 || layer.Height <= 0) && !string.IsNullOrEmpty(layer.Icon))
                {
                    Texture2D texture = ResourceAtlas.GetTexture(layer.Icon);
                    if (texture != null)
                    {
                        layer.Width = layer.Width > 0 ? layer.Width : texture.Width;
                        layer.Height = layer.Height > 0 ? layer.Height : texture.Height;
                    }
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            //DrawDebug(sb);

            unlockBurstParticles.Draw(sb);
            foreach (LinkParticleSystem particles in linkParticles.Values)
                particles.Draw(sb, zoom);

            DrawHoveredTokenPortal(sb);
            //DrawHoveredTokenRing(sb);

            var tokens = treeTokens.Values.ToList();

            for (int i = 0; i < treeTokens.Values.Count; i++)
            {
                SkillTreeToken token = tokens[i];

                Color drawColor =
                    token.IsCollected ? Color.White :
                    token.IsCollectable ? Color.White * .25f : Color.White * .05f;

                Vector2 position = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
                for (int layerIndex = 0; layerIndex < token.IconLayers.Count; layerIndex++)
                {
                    IconLayer layer = token.IconLayers[layerIndex];
                    DrawTokenIcon(sb, layer.Icon, layer.Width, layer.Height, position, drawColor, .5f + layerIndex * .01f);
                }

            }
        }

        private void DrawTokenIcon(
            SpriteBatch sb,
            string iconName,
            int width,
            int height,
            Vector2 position,
            Color drawColor,
            float layerDepth)
        {
            if (string.IsNullOrEmpty(iconName) || width <= 0 || height <= 0)
                return;

            Texture2D tex = ResourceAtlas.GetTexture(iconName);
            if (tex == null)
                return;

            Vector2 iconSize = new Vector2(width, height) * zoom;
            Rectangle tokenRect = new Rectangle(
                (position - iconSize / 2f).ToPoint(),
                iconSize.ToPoint());

            sb.Draw(tex, tokenRect, null, drawColor, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        }

        private void DrawHoveredTokenRing(SpriteBatch sb)
        {
            if (hoverAuraTokenId < 0 || !treeTokens.TryGetValue(hoverAuraTokenId, out SkillTreeToken token) || hoverAuraAlpha <= .001f)
                return;

            Vector2 center = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
            float auraRadius = HoverAuraRadius * zoom;
            Color color = GetHoverEffectColor(token);
            Effect effect = ResourceAtlas.GetEffect("SkillTreeHoverAura");

            sb.End();
            effect.Parameters["iTime"]?.SetValue(time);
            effect.Parameters["ringTimeOffsets"]?.SetValue(hoverAuraTimeOffsets);
            Renderer.ResetBeginDraw(sb, effect, DrawSpace.World);
            float diameter = auraRadius * 2f;
            sb.Draw(Drawing.Pixel, new Rectangle((center - Vector2.One * auraRadius).ToPoint(), new Point((int)diameter)), color);

            sb.End();
            Renderer.ResetBeginDraw(sb, drawSpace: DrawSpace.World);
        }

        private void DrawHoveredTokenPortal(SpriteBatch sb)
        {
            if (hoverAuraTokenId < 0 || !treeTokens.TryGetValue(hoverAuraTokenId, out SkillTreeToken token) || hoverAuraAlpha <= .001f)
                return;

            Vector2 center = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
            int largestIconDimension = token.IconLayers.Max(layer => Math.Max(layer.Width, layer.Height));
            float portalSize = token.PortalSize > 0 ? token.PortalSize : largestIconDimension;
            float radius = Math.Max(1f, portalSize * zoom * .75f);
            Color color = GetHoverEffectColor(token);
            Effect effect = ResourceAtlas.GetEffect("SkillTreeHoverPortal");

            sb.End();
            effect.Parameters["iTime"]?.SetValue(time);
            Renderer.ResetBeginDraw(sb, effect, DrawSpace.World);
            float diameter = radius * 2f;
            sb.Draw(Drawing.Pixel, new Rectangle((center - Vector2.One * radius).ToPoint(), new Point((int)diameter)), color);

            sb.End();
            Renderer.ResetBeginDraw(sb, drawSpace: DrawSpace.World);
        }

        private Color GetHoverEffectColor(SkillTreeToken token)
        {
            float iconAlpha = token.IsCollected ? 1f : token.IsCollectable ? .25f : .05f;
            return Color.White * (iconAlpha * .5f * hoverAuraAlpha);
        }

        private void DrawLinkPulseLines(SpriteBatch sb, Effect effect)
        {
            foreach (SkillTreeToken child in treeTokens.Values)
            {
                if (!CanPulseLink(child))
                    continue;

                foreach (int parentId in child.ParentTokenIDs)
                {
                    if (!treeTokens.ContainsKey(parentId))
                        continue;

                    Vector2 start = GetWorldPosition(treeTokens[parentId].GridPosition).ToVector2() * zoom;
                    Vector2 end = GetWorldPosition(child.GridPosition).ToVector2() * zoom;
                    Color color = GetLinkColor(child.TokenID);
                    effect.Parameters["pulseDelay"]?.SetValue(treeTokens[parentId].Depth * LinkPulseTravelTime);
                    sb.DrawLineCentered(start, end, LinkLineThickness * zoom, color, .2f);
                }
            }
        }

        private void UnlockSkill(int tokenId)
        {
            SkillTreeToken token = GetToken(tokenId);

            token.Collect();
        }

        private void DrawDebug(SpriteBatch sb)
        {
            int gridSize = 100;
            float cellSize = GridSpacing;

            float gridWidth = gridSize * cellSize;
            float gridHeight = gridSize * cellSize;

            Vector2 startPos = new Vector2(-gridWidth / 2f, -gridHeight / 2f);

            // Horizontal lines
            for (int y = 0; y <= gridSize; y++)
            {
                Vector2 pos1 = startPos + new Vector2(0, y * cellSize);
                Vector2 pos2 = startPos + new Vector2(gridWidth, y * cellSize);

                sb.DrawLineCentered(pos1, pos2, 1, Color.White * 0.05f, 0.15f);
            }

            // Vertical lines
            for (int x = 0; x <= gridSize; x++)
            {
                Vector2 pos1 = startPos + new Vector2(x * cellSize, 0);
                Vector2 pos2 = startPos + new Vector2(x * cellSize, gridHeight);

                sb.DrawLineCentered(pos1, pos2, 1, Color.White * 0.05f, 0.15f);
            }

            foreach (SkillTreeToken token in treeTokens.Values)
            {
                sb.DrawString(ResourceAtlas.GetFont("DePixelHalbfett"), token.TokenID.ToString(), token.GridPosition.ToVector2() * GridSpacing, Color.Black, 0, -Vector2.One * IconSize / 2, .35f, SpriteEffects.None, .75f);
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            unlockBurstParticles.SlowUpdate(gameTime);
            foreach (SkillTreeToken token in treeTokens.Values)
                token.IsCollectable = token.ParentTokenIDs.Count == 0 ||
                    token.ParentTokenIDs.All(parentId =>
                        treeTokens.TryGetValue(parentId, out SkillTreeToken parent) && parent.IsCollected);

        }

        public void StandardUpdate(GameTime gameTime)
        {
            unlockBurstParticles.SetBoundsSize(zoom);

            time = (float)gameTime.TotalGameTime.TotalSeconds;
            UpdateHoverAura((float)gameTime.ElapsedGameTime.TotalSeconds);
            if (Shockwave != null)
                Shockwave.Time = time;
            unlockBurstParticles.StandardUpdate(gameTime);
            UpdateLinkParticles(gameTime);

            zoomTarget = Math.Clamp(zoomTarget, 0.5f, 2f);
            zoom = MathHelper.Lerp(zoom, zoomTarget, .25f);

            if (editing) return;

            if (Input.IsMouseInPresentationBounds && Input.IsRightButtonDownOnce())
            {
                mouseStart = (Input.GetMouseScreenPos().ToVector2() / zoom).ToPoint();
                cameraStart = GetCurrentCameraTargetPosition();
                lastCameraPosition = cameraStart;
            }

            if (Input.IsMouseInPresentationBounds && Input.IsRightButtonDown())
            {
                Point mouseCurrent = (Input.GetMouseScreenPos().ToVector2() / zoom).ToPoint();
                Point delta = mouseStart - mouseCurrent;
                Point newPos = cameraStart + delta;

                newPos.X = (int)Math.Clamp(newPos.X, -500 * zoom, 500 * zoom);
                newPos.Y = (int)Math.Clamp(newPos.Y, -500 * zoom, 500 * zoom);

                Renderer.CurrentCamera.SetTarget(newPos);
                lastCameraPosition = newPos;
            }

            if (Input.IsButtonDown(Keys.LeftControl))
            {
                if (Input.IsButtonDownOnce(Keys.S))
                    SaveSkillTree();

                if (Input.IsButtonDownOnce(Keys.Delete))
                    ResetSkillTree();
            }

            if (Input.IsMouseInPresentationBounds)
                zoomTarget -= Input.GetMouseScrollDelta() * .25f;
        }

        private static Point GetCurrentCameraTargetPosition()
        {
            Point cameraPosition = Renderer.CurrentCamera.Position;
            Point renderSize = Renderer.RenderSize;
            return new Point(renderSize.X / 2 - cameraPosition.X, renderSize.Y / 2 - cameraPosition.Y);
        }

        private void UpdateHoverAura(float elapsedSeconds)
        {
            int hoveredTokenId = Input.IsMouseInPresentationBounds && TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken token)
                ? token.TokenID
                : -1;
            float targetAlpha = hoveredTokenId >= 0 ? 1f : 0f;

            if (hoveredTokenId >= 0 && hoveredTokenId != hoverAuraTokenId)
            {
                hoverAuraTokenId = hoveredTokenId;
                hoverAuraAlpha = 0f;
                hoverAuraTimeOffsets = new Vector4(
                    RandomHelper.Instance.GetFloat(0f, 20f),
                    RandomHelper.Instance.GetFloat(0f, 20f),
                    RandomHelper.Instance.GetFloat(0f, 20f),
                    RandomHelper.Instance.GetFloat(0f, 20f));
            }

            hoverAuraAlpha = MathHelper.Lerp(hoverAuraAlpha, targetAlpha, 1f - MathF.Exp(-5f * elapsedSeconds));
            if (hoverAuraAlpha <= .01f && targetAlpha == 0f)
                hoverAuraTokenId = -1;
        }

        public bool TryGetTokenAt(Vector2 mousePosition, out SkillTreeToken hitToken)
        {
            foreach (SkillTreeToken token in treeTokens.Values.OrderByDescending(token => token.Depth))
            {
                EnsureIconLayers(token);
                int largestDimension = token.IconLayers.Max(layer => Math.Max(layer.Width, layer.Height));
                if (largestDimension <= 0)
                    continue;

                Vector2 center = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
                float halfSize = largestDimension * zoom / 2f;
                Rectangle bounds = new Rectangle((center - Vector2.One * halfSize).ToPoint(), new Point((int)(halfSize * 2f)));
                if (bounds.Contains(mousePosition))
                {
                    hitToken = token;
                    return true;
                }
            }

            hitToken = null;
            return false;
        }

        public void ControlledUpdate(GameTime gameTime) => unlockBurstParticles.ControlledUpdate(gameTime);

        private ParticleSystem CreateUnlockBurstParticles()
        {
            TrailInfo trail = new TrailInfo
            {
                TrailLength = 48f,
                NumberOfSegments = 16,
                TipWidth = 3,
                EndWidth = 0,
                SegmentsPerSecond = 24f,
                SegmentsRemovedPerSecond = 18f,
                SegmentColor = _ => Color.White * .7f,
                TrackLayerDepth = () => .4f
            };

            ParticleSystemStats stats = new ParticleSystemStats
            {
                SpawnBounds = new[] { new[] { new Rectangle(-16, -16, 32, 32) } },
                CurrentBounds = 0,
                UseRandomBounds = false,
                SpawnShape = ParticleSpawnShape.Circle,
                SpawnPlacement = ParticleSpawnPlacement.Edge,
                ParticleLifeSpan = new[] { .5175f, .73f },
                TrackPosition = () => unlockBurstPosition,
                ParticleDespawnDistance = 1000f,
                TrackLayerDepth = () => .24f,
                MaxParticleCount = 80,
                ParticleStartColor = new[] { Color.White },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleTextureKeys = new[] { "square" },
                ParticleSpeed = new[] { .1f, .5f },
                ParticleSize = new[] { .05f, .1f },
                EmitRate = new[] { 0f },
                EmitCount = new[] { 28 },
                ParticleRotation = new[] { 0f, MathF.PI * 2 },
                ParticleRotationSpeed = _ => 0f,
                ParticleColorDecayRate = t => t,
                ParticleSizeDecayRate = t => (1f - t) * zoom,
                TrackStartingVelocity = () => { return RandomHelper.Instance.GetVector2(new Vector2(-2.5f), new Vector2(2.5f)) * zoom; },
                ActingForce = _ => RandomHelper.Instance.GetVector2(new Vector2(-.8f), new Vector2(.8f)) * zoom /*+ Vector2.UnitY * .015f*/,
                ResetParticlesAfterDeath = false,
                Trail = trail
            };

            return new ParticleSystem(stats);
        }

        public void UnlockToken(int tokenId)
        {
            SkillTreeToken token = treeTokens[tokenId];
            if (token.IsCollected)
                return;

            token.Collect();
            Shockwave?.Add(GetWorldPosition(token.GridPosition).ToVector2() * zoom);
            unlockBurstPosition = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
            unlockBurstParticles.EmitParticles();
        }

        private void UpdateLinkParticles(GameTime gameTime)
        {
            HashSet<(int Parent, int Child)> activeLinks = new();
            foreach (SkillTreeToken child in treeTokens.Values)
            {
                if (!CanFlowLinkParticles(child))
                    continue;

                foreach (int parentId in child.ParentTokenIDs)
                {
                    if (!treeTokens.ContainsKey(parentId))
                        continue;

                    var link = (Parent: parentId, Child: child.TokenID);
                    activeLinks.Add(link);
                    if (!linkParticles.TryGetValue(link, out LinkParticleSystem particles))
                    {
                        particles = new LinkParticleSystem(LinkParticleSettings);
                        linkParticles.Add(link, particles);
                    }

                    particles.Update(gameTime, GetLinkStart(link), GetLinkEnd(link), GetLinkColor(link.Child));
                }
            }

            foreach (var link in linkParticles.Keys.Where(link => !activeLinks.Contains(link)).ToList())
                linkParticles.Remove(link);
        }

        private Vector2 GetLinkStart((int Parent, int Child) link) =>
            GetWorldPosition(treeTokens[link.Parent].GridPosition).ToVector2() * zoom;

        private Vector2 GetLinkEnd((int Parent, int Child) link) =>
            GetWorldPosition(treeTokens[link.Child].GridPosition).ToVector2() * zoom;

        private Color GetLinkColor(int childId)
        {
            SkillTreeToken child = treeTokens[childId];
            return child.IsCollected ? Color.White : child.IsCollectable ? Color.White * .25f : Color.White * .05f;
        }

        private static bool CanPulseLink(SkillTreeToken child) =>
            child.IsCollected || child.IsCollectable;

        private static bool CanFlowLinkParticles(SkillTreeToken child) =>
            child.IsCollected;

        public void CollectToken(Point gridPosition)
        {
            UnlockToken(tokenPositions[gridPosition].TokenID);
        }

        public void AddToken(Vector2 worldPosition)
        {
            Point gridPosition = GetGridPosition(worldPosition);
            AddToken(gridPosition);
        }

        public SkillTreeToken AddToken(Point gridPosition)
        {
            if (CheckForToken(gridPosition)) return null;

            if (treeTokens == null)
                treeTokens = new();

            int id = TokenID;

            SkillTreeToken token = new SkillTreeToken(DefaultIcon, gridPosition, id);
            token.IconLayers[0].Width = IconSize;
            token.IconLayers[0].Height = IconSize;
            treeTokens.Add(id, token);
            tokenPositions.Add(gridPosition, token);
            TokenID++;

            return token;
        }

        public void RemoveToken(Point gridPosition)
        {
            if (!CheckForToken(gridPosition)) return;

            int id = tokenPositions[gridPosition].TokenID;
            treeTokens.Remove(id);
            tokenPositions.Remove(gridPosition);
            foreach (SkillTreeToken token in treeTokens.Values)
            {
                token.RemoveParentToken(id);
                token.RemoveChildToken(id);
            }

            SetFamilyTokens();
        }

        public bool CheckForToken(Point gridPosition)
        {
            return tokenPositions.ContainsKey(gridPosition);
        }

        public int GetTokenID(Point gridPosition) => tokenPositions[gridPosition].TokenID;
        public SkillTreeToken GetToken(Point gridPosition) => tokenPositions[gridPosition];
        public SkillTreeToken GetToken(int id) => treeTokens[id];

        public void AddEffect(Point gridPosition, SkillEffectDefinition effect)
        {
            SkillTreeToken token = GetToken(gridPosition);
            token.Effects ??= new List<SkillEffectDefinition>();
            token.Effects.Add(effect);
        }

        public void AddEffect(int tokenId, SkillEffectDefinition effect)
        {
            SkillTreeToken token = treeTokens[tokenId];
            token.Effects ??= new List<SkillEffectDefinition>();
            token.Effects.Add(effect);
        }

        public void AddResourceCost(Point gridPosition, string resource, int amount) =>
            AddResourceCost(GetTokenID(gridPosition), resource, amount);

        public void AddResourceCost(int tokenId, string resource, int amount)
        {
            SkillTreeToken token = treeTokens[tokenId];
            token.ResourceCosts ??= new List<ResourceCost>();
            token.ResourceCosts.Add(new ResourceCost
            {
                Resource = resource,
                Amount = Math.Max(0, amount)
            });
        }

        public void SetTokenParent(SkillTreeToken parentToken, SkillTreeToken childToken)
        {
            if (parentToken == null || childToken == null || parentToken == childToken ||
                childToken.ParentTokenIDs.Contains(parentToken.TokenID))
                return;

            childToken.SetParentToken(parentToken.TokenID);
            parentToken.SetChildToken(childToken.TokenID);

            parentToken.ChildTokens.Add(childToken);
            childToken.ParentTokens.Add(parentToken);
            RefreshTokenDepths();
        }

        private void RefreshTokenDepths()
        {
            Dictionary<int, int> depths = new();
            foreach (SkillTreeToken token in treeTokens.Values)
                token.Depth = CalculateTokenDepth(token, new HashSet<int>(), depths);
        }

        private int CalculateTokenDepth(SkillTreeToken token, HashSet<int> visited, Dictionary<int, int> depths)
        {
            if (depths.TryGetValue(token.TokenID, out int cachedDepth))
                return cachedDepth;

            if (!visited.Add(token.TokenID) || token.ParentTokenIDs.Count == 0)
                return 0;

            int depth = int.MaxValue;
            foreach (int parentId in token.ParentTokenIDs)
            {
                if (treeTokens.TryGetValue(parentId, out SkillTreeToken parent))
                    depth = Math.Min(depth, CalculateTokenDepth(parent, new HashSet<int>(visited), depths) + 1);
            }

            int result = depth == int.MaxValue ? 0 : depth;
            depths[token.TokenID] = result;
            return result;
        }

        public void SetTokenParent(Point parentPos, Point childPos)
        {
            List<SkillTreeToken> tokens = treeTokens.Values.ToList();
            SkillTreeToken parentToken = tokens.Find(x => x.GridPosition == parentPos);
            SkillTreeToken childToken = tokens.Find(x => x.GridPosition == childPos);

            SetTokenParent(parentToken, childToken);
        }

        public Point GetGridPosition()
        {
            return GetGridPosition(Input.GetMousePos().ToVector2());
        }

        public Point GetGridPosition(Vector2 worldPosition)
        {
            worldPosition /= zoom;
            int offset = GridSpacing / 2;
            worldPosition += new Vector2(worldPosition.X < 0 ? -offset : offset, worldPosition.Y < 0 ? -offset : offset);
            return (worldPosition / GridSpacing).ToPoint();
        }

        public Point GetWorldPosition(Point gridPosition)
        {
            return (gridPosition.ToVector2() * GridSpacing).ToPoint();
        }

        public void LoadSkillTree()
        {
            FileIO.ReadJsonInto(this, "Content/SaveData/SkillTree");

            tokenPositions = new();

            foreach (SkillTreeToken token in treeTokens.Values)
            {
                token.ResourceCosts ??= new List<ResourceCost>();
                token.Effects ??= new List<SkillEffectDefinition>();
                EnsureIconLayers(token);

                tokenPositions.Add(token.GridPosition, token);
            }
        }
        public void SaveSkillTree()
        {
            FileIO.WriteJsonTo(this, "Content/SaveData/SkillTree", Newtonsoft.Json.Formatting.Indented);
        }

        public void ResetSkillTree()
        {
            treeTokens = new();
            tokenPositions = new();
        }
    }
}
