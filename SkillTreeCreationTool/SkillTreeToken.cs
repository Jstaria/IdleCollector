using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public sealed class ResourceCost
    {
        public string Resource { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public sealed class IconLayer
    {
        public string Icon { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public Color Tint { get; set; } = Color.White;
    }

    public class SkillTreeToken
    {
        private const float RevealDuration = .12f;
        private const float SpringTimeStep = 1f / 60f;
        [JsonIgnore] private float revealProgress;
        [JsonIgnore] private readonly Spring revealScaleSpring = new Spring(38f, .65f, 1f) { Position = 0f };
        [JsonIgnore] private readonly Spring unlockScaleSpring = new Spring(22f, .85f, 1f);
        [JsonIgnore] private readonly Spring unlockDepthSpring = new Spring(18f, 1f, 0f);
        [JsonIgnore] private float unlockDepthHold;
        [JsonIgnore] private float springElapsed;
        [JsonIgnore] internal bool IsUnlockElevated => unlockDepthHold > 0f;
        [JsonIgnore] internal float IconDrawScale => revealScaleSpring.Position * unlockScaleSpring.Position;

        public List<IconLayer> IconLayers { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public int PortalSize { get; set; } = 32;
        public int Depth { get; set; }
        public int TokenID { get; set; }
        public Point GridPosition { get; set; }
        public List<int> ParentTokenIDs { get; set; }
        public List<int> ChildTokenIDs { get; set; }
        public List<ResourceCost> ResourceCosts { get; set; }
        public List<SkillEffectDefinition> Effects { get; set; }
        [JsonIgnore] public List<SkillTreeToken> ChildTokens { get; set; }
        [JsonIgnore] public List<SkillTreeToken> ParentTokens { get; set; }

        public bool IsCollected { get; set; }
        public bool IsCollectable { get; set; }

        public SkillTreeToken(string iconName, Point gridPosition, int tokenID)
        {
            IconLayers = new List<IconLayer> { new IconLayer { Icon = iconName } };

            TokenID = tokenID;
            GridPosition = gridPosition;
            ParentTokenIDs = new List<int>();
            ChildTokenIDs = new List<int>();
            ResourceCosts = new List<ResourceCost>();
            Effects = new List<SkillEffectDefinition>();
            ChildTokens = new List<SkillTreeToken>();
            ParentTokens = new List<SkillTreeToken>();
            IsCollected = false;
        }

        public void UpdateIconAnimation(float elapsedSeconds)
        {
            if (!IsCollectable && !IsCollected)
            {
                revealProgress = springElapsed = 0f;
                revealScaleSpring.Position = revealScaleSpring.Velocity = 0f;
                unlockScaleSpring.Position = 1f;
                unlockScaleSpring.Velocity = 0f;
                unlockDepthHold = 0f;
                unlockDepthSpring.Position = unlockDepthSpring.Velocity = unlockDepthSpring.RestPosition = 0f;
                return;
            }

            revealProgress = Math.Min(1f, revealProgress + elapsedSeconds / RevealDuration);
            unlockDepthHold = Math.Max(0f, unlockDepthHold - elapsedSeconds);
            unlockDepthSpring.RestPosition = unlockDepthHold > 0f ? 1f : 0f;
            springElapsed += elapsedSeconds;
            while (springElapsed >= SpringTimeStep)
            {
                revealScaleSpring.Update();
                unlockScaleSpring.Update();
                unlockDepthSpring.Update();
                springElapsed -= SpringTimeStep;
            }
        }

        public void DrawIcon(SpriteBatch sb, Vector2 position, float zoom, bool highlightMask = false, float opacity = 1f)
        {
            if (!IsCollectable && !IsCollected)
                return;

            float revealScale = IconDrawScale;
            if (revealScale <= 0f)
                return;

            float revealAlpha = MathHelper.SmoothStep(0f, 1f, revealProgress);
            Color drawColor = (highlightMask ? Color.White : IsCollected ? Color.White : Color.White * .25f) * (revealAlpha * opacity);
            for (int layerIndex = 0; layerIndex < IconLayers.Count; layerIndex++)
            {
                IconLayer layer = IconLayers[layerIndex];
                if (string.IsNullOrEmpty(layer.Icon) || layer.Width <= 0 || layer.Height <= 0)
                    continue;

                Texture2D texture = ResourceAtlas.GetTexture(layer.Icon);
                if (texture == null)
                    continue;

                Color tint = Color.FromNonPremultiplied(layer.Tint.ToVector4());
                Color layerColor = new Color(tint.ToVector4() * drawColor.ToVector4());
                Vector2 iconSize = new Vector2(layer.Width, layer.Height) * zoom * revealScale;
                Rectangle bounds = new Rectangle((position - iconSize / 2f).ToPoint(), iconSize.ToPoint());
                float layerStep = Math.Min(.01f, .08f / Math.Max(1, IconLayers.Count - 1));
                float depth = .5f + .4f * Math.Clamp(unlockDepthSpring.Position, 0f, 1f) + layerIndex * layerStep;
                sb.Draw(texture, bounds, null, layerColor, 0f, Vector2.Zero, SpriteEffects.None, depth);
            }
        }

        public void SetParentToken(int parent) => ParentTokenIDs.Add(parent);
        public void RemoveParentToken(int parent) => ParentTokenIDs.Remove(parent);

        public void SetChildToken(int child) => ChildTokenIDs.Add(child);
        public void RemoveChildToken(int child) => ChildTokenIDs.Remove(child);

        public void Collect()
        {
            if (IsCollected)
                return;

            IsCollected = true;
            revealScaleSpring.Position = revealScaleSpring.RestPosition = 1f;
            revealScaleSpring.Velocity = 0f;
            unlockScaleSpring.SetValues(18f, 1f);
            unlockScaleSpring.Position = 1.65f;
            unlockScaleSpring.Velocity = 0f;
            // Lift immediately so even the first flash frame stays behind this icon.
            unlockDepthHold = TokenUnlockWisp.BurnDuration + TokenUnlockWisp.FadeDuration;
            unlockDepthSpring.Position = unlockDepthSpring.RestPosition = 1f;
            unlockDepthSpring.Velocity = 0f;
        }
    }
}
