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
        private const float MinimumZoom = .5f;
        private const float MaximumZoom = 2f;
        private const float ZoomStep = .5f;
        private static readonly Point DescriptionBoxInset = new Point(24, 28);
        private static readonly Vector2 DescriptionTextPadding = new Vector2(12f, 10f);
        private static readonly Point UnlockButtonSize = new Point(48, 12);
        private const int CostIconWorldSize = 14;

        [JsonRequired] public int GridSpacing = 100;
        [JsonRequired] public int IconSize = 50;
        [JsonIgnore] public Point IconSizePoint;
        [JsonRequired] public string DefaultIcon = "back10";
        [JsonRequired] public int TokenID = 0;
        [JsonIgnore] public Vector2 iconOrigin;
        [JsonIgnore] public Texture2D iconTexture;

        [JsonProperty] private Dictionary<int, SkillTreeToken> treeTokens;
        [JsonIgnore] private Dictionary<Point, SkillTreeToken> tokenPositions;
        [JsonIgnore] private readonly Dictionary<(int Parent, int Child), LinkParticleSystem> linkParticles = new();
        [JsonIgnore] public LinkParticleSettings LinkParticleSettings { get; } = new();
        [JsonIgnore] private SkillTreeShockwave Shockwave { get; }
        [JsonIgnore] private readonly SkillTreeHoverHighlight hoverHighlight;
        [JsonIgnore] private readonly ParticleSystemGroup unlockParticles = new();
        [JsonIgnore] private readonly List<TokenUnlockWisp> unlockWisps = new();
        [JsonIgnore] private Vector2 unlockBurstPosition;

        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public Color Color { get; set; }


        private Point mouseStart;
        private Point cameraStart;
        private bool draggingCamera;
        private Point lastCameraPosition;

        private BasicEffectRenderable _renderable;
        private LinkPulseRenderable linkPulseRenderable;
        public string SkillTreeScene = "SkillTreeScene";
        [JsonIgnore] private float time;
        [JsonIgnore] private int hoverAuraTokenId = -1;
        [JsonIgnore] private float hoverAuraAlpha;
        [JsonIgnore] private Vector4 hoverAuraTimeOffsets;
        [JsonIgnore] private CustomText hoverDescriptionText;
        [JsonIgnore] private Button unlockButton;
        [JsonIgnore] private Rectangle hoverDescriptionBounds;
        [JsonIgnore] private Rectangle hoverUnlockButtonBounds;
        [JsonIgnore] private int selectedDescriptionTokenId = -1;
        [JsonIgnore] private int hoverDescriptionTokenId = -1;
        [JsonIgnore] private float hoverDescriptionAlpha;
        [JsonIgnore] public float zoom = 1;
        [JsonIgnore] public float zoomTarget = 1;
        [JsonIgnore] public bool editing = false;
        [JsonIgnore] public Func<IReadOnlyDictionary<string, int>, bool> TrySpendResources { get; set; }


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

            unlockParticles.Add(CreateUnlockBurstParticles());
            unlockParticles.Add(CreateUnlockWaveParticles());
            unlockParticles.Add(CreateUnlockFlashParticles(.75f));
            unlockParticles.Add(CreateUnlockFlashParticles(.55f));
            Shockwave = new SkillTreeShockwave();
            Renderer.AddPostProcess("SkillTreeShockwave", Shockwave);
            hoverHighlight = new SkillTreeHoverHighlight(
                token => GetWorldPosition(token.GridPosition).ToVector2() * zoom,
                () => zoom, SkillTreeScene);
            Renderer.AddPostProcess("SkillTreeHoverHighlight", hoverHighlight);
            Renderer.AddToSceneUIDraw(SkillTreeScene, DrawDescriptionUi);
            unlockButton = new Button(Game1.Instance, new ButtonConfig
            {
                bounds = new Rectangle(Point.Zero, UnlockButtonSize * Renderer.UIScaler),
                textures = new[] { ResourceAtlas.GetTexture("GUI_Button") },
                texts = new[] { "Unlock", "<fx 0,0,0,0,1>></fx> Unlock <fx 0,0,0,0,2><</fx>" },
                font = "DePixelHalbfett",
                fontColor = Color.White,
                shadowColor = Color.Black * .45f,
                textOffset = new Vector2(2f, 4f),
                OnClick = () =>
                {
                    if (hoverDescriptionTokenId >= 0 && treeTokens.TryGetValue(hoverDescriptionTokenId, out SkillTreeToken token))
                        TryPurchaseToken(token);
                }
            });
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

            //DrawHoveredTokenPortal(sb);
            foreach (LinkParticleSystem particles in linkParticles.Values)
                particles.Draw(sb, zoom);

            DrawUnlockWisps(sb);
            unlockParticles.Draw(sb);

            //DrawHoveredTokenRing(sb);

            foreach (SkillTreeToken token in treeTokens.Values)
                if (unlockWisps.Count == 0 || token.IsUnlockElevated)
                    token.DrawIcon(sb, GetWorldPosition(token.GridPosition).ToVector2() * zoom, zoom);

            DrawHoverDescription(sb);
        }

        private void DrawUnlockWisps(SpriteBatch sb)
        {
            if (unlockWisps.Count == 0)
                return;

            foreach (SkillTreeToken token in treeTokens.Values)
                if (!token.IsUnlockElevated)
                    token.DrawIcon(sb, GetWorldPosition(token.GridPosition).ToVector2() * zoom, zoom);

            sb.End();
            Effect effect = ResourceAtlas.GetEffect("SkillTreeUnlockWisp");
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect, Renderer.CurrentCamera.Transform);
            foreach (TokenUnlockWisp wisp in unlockWisps)
                wisp.Draw(sb, effect, zoom);
            sb.End();
            Renderer.ResetBeginDraw(sb, drawSpace: DrawSpace.World);
            foreach (TokenUnlockWisp wisp in unlockWisps)
                wisp.DrawDust(sb);
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
            float portalSize = GetPortalSize(token);
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
            unlockParticles.SlowUpdate(gameTime);
            foreach (SkillTreeToken token in treeTokens.Values)
                token.IsCollectable = token.ParentTokenIDs.Count == 0 ||
                    token.ParentTokenIDs.All(parentId =>
                        treeTokens.TryGetValue(parentId, out SkillTreeToken parent) && parent.IsCollected);

        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (SkillTreeToken token in treeTokens.Values)
                token.UpdateIconAnimation((float)gameTime.ElapsedGameTime.TotalSeconds);

            foreach (ParticleSystem particles in unlockParticles.Systems)
                particles.SetBoundsSize(zoom);

            time = (float)gameTime.TotalGameTime.TotalSeconds;
            UpdateHoverAura(gameTime);
            if (Shockwave != null)
                Shockwave.Time = time;
            unlockParticles.StandardUpdate(gameTime);
            for (int i = unlockWisps.Count - 1; i >= 0; i--)
            {
                TokenUnlockWisp wisp = unlockWisps[i];
                wisp.Update(gameTime, zoom);
                if (wisp.Finished)
                {
                    unlockWisps.RemoveAt(i);
                }
            }
            UpdateLinkParticles(gameTime);

            zoomTarget = Math.Clamp(MathF.Round(zoomTarget / ZoomStep) * ZoomStep, MinimumZoom, MaximumZoom);
            zoom = MathHelper.Lerp(zoom, zoomTarget, .25f);

            if (editing)
            {
                draggingCamera = false;
                return;
            }

            bool controlDown = Input.IsButtonDown(Keys.LeftControl) || Input.IsButtonDown(Keys.RightControl);
            if (!Input.IsMiddleButtonDown() || controlDown)
                draggingCamera = false;

            if (Input.IsMouseInPresentationBounds && Input.IsMiddleButtonDownOnce() && !controlDown)
            {
                draggingCamera = true;
                mouseStart = (Input.GetMouseScreenPos().ToVector2() / zoom).ToPoint();
                cameraStart = GetCurrentCameraTargetPosition();
                lastCameraPosition = cameraStart;
            }

            if (Input.IsMouseInPresentationBounds && draggingCamera && Input.IsMiddleButtonDown())
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
                zoomTarget = Math.Clamp(zoomTarget - Input.GetMouseScrollDelta() * ZoomStep, MinimumZoom, MaximumZoom);
        }

        private static Point GetCurrentCameraTargetPosition()
        {
            Point cameraPosition = Renderer.CurrentCamera.Position;
            Point renderSize = Renderer.RenderSize;
            return new Point(renderSize.X / 2 - cameraPosition.X, renderSize.Y / 2 - cameraPosition.Y);
        }

        private void UpdateHoverAura(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int hoveredTokenId = Input.IsMouseInPresentationBounds && TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken token)
                ? token.TokenID
                : -1;

            float targetAlpha = hoveredTokenId >= 0 ? 1f : 0f;
            hoverHighlight.Update(hoveredTokenId >= 0 ? treeTokens[hoveredTokenId] : null, elapsedSeconds);
            UpdateHoverDescription(gameTime);

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

        private void UpdateHoverDescription(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (editing)
                selectedDescriptionTokenId = -1;
            else if (Input.IsMouseInPresentationBounds && Input.IsLeftButtonDownOnce())
            {
                Point mousePoint = Input.GetMousePos();
                bool canClickUnlock = selectedDescriptionTokenId >= 0 &&
                    hoverDescriptionAlpha > .1f &&
                    treeTokens.TryGetValue(selectedDescriptionTokenId, out SkillTreeToken currentToken) &&
                    currentToken.IsCollectable && !currentToken.IsCollected &&
                    hoverUnlockButtonBounds.Contains(mousePoint);
                if (!canClickUnlock)
                {
                    if (TryGetTokenAt(mousePoint.ToVector2(), out SkillTreeToken clickedToken))
                    {
                        selectedDescriptionTokenId = selectedDescriptionTokenId == clickedToken.TokenID
                            ? -1
                            : clickedToken.TokenID;
                    }
                    else
                    {
                        selectedDescriptionTokenId = -1;
                    }
                }
            }

            SkillTreeToken activeToken = !editing && selectedDescriptionTokenId >= 0 &&
                treeTokens.TryGetValue(selectedDescriptionTokenId, out SkillTreeToken selectedToken)
                ? selectedToken
                : null;
            if (activeToken != null && activeToken.TokenID != hoverDescriptionTokenId)
            {
                hoverDescriptionTokenId = activeToken.TokenID;
                hoverDescriptionAlpha = 0f;
            }

            float targetAlpha = activeToken != null ? 1f : 0f;
            hoverDescriptionAlpha = MathHelper.Lerp(hoverDescriptionAlpha, targetAlpha, 1f - MathF.Exp(-8f * elapsedSeconds));
            if (editing || (hoverDescriptionAlpha <= .01f && targetAlpha == 0f) ||
                hoverDescriptionTokenId < 0 || !treeTokens.TryGetValue(hoverDescriptionTokenId, out SkillTreeToken displayToken))
            {
                selectedDescriptionTokenId = -1;
                hoverDescriptionTokenId = -1;
                hoverDescriptionAlpha = 0f;
                return;
            }

            Texture2D boxTexture = ResourceAtlas.GetTexture("GUI_TextBox2");
            Point boxSize = boxTexture.Bounds.Size;
            Rectangle cameraBounds = Renderer.CameraBounds;
            Vector2 tokenWorldPos = GetWorldPosition(displayToken.GridPosition).ToVector2() * zoom;
            float tokenRadius = GetPortalSize(displayToken) * zoom * .5f;
            int x = (int)MathF.Round(tokenWorldPos.X - boxSize.X / 2f);
            int y = (int)MathF.Round(tokenWorldPos.Y - tokenRadius - boxSize.Y + 12f);
            if (y < cameraBounds.Top + 4)
                y = (int)MathF.Round(tokenWorldPos.Y + tokenRadius - 12f);

            x = Math.Clamp(x, cameraBounds.Left + 4, cameraBounds.Right - boxSize.X - 4);
            y = Math.Clamp(y, cameraBounds.Top + 4, cameraBounds.Bottom - boxSize.Y - 4);
            hoverDescriptionBounds = new Rectangle(x, y, boxSize.X, boxSize.Y);
            hoverUnlockButtonBounds = new Rectangle(
                hoverDescriptionBounds.Center.X - UnlockButtonSize.X / 2,
                hoverDescriptionBounds.Bottom - DescriptionBoxInset.Y - UnlockButtonSize.Y - 2,
                UnlockButtonSize.X,
                UnlockButtonSize.Y);

            bool showUnlockButton = displayToken.IsCollectable && !displayToken.IsCollected;
            bool hasCosts = displayToken.ResourceCosts != null && displayToken.ResourceCosts.Count > 0;
            int bottomReservedWorld = (showUnlockButton ? UnlockButtonSize.Y + 4 : 0) + (hasCosts ? CostIconWorldSize + 4 : 0);

            Vector2 worldInnerPosition = new Vector2(
                hoverDescriptionBounds.X + DescriptionBoxInset.X,
                hoverDescriptionBounds.Y + DescriptionBoxInset.Y);
            Vector2 worldInnerDimension = new Vector2(
                hoverDescriptionBounds.Width - DescriptionBoxInset.X * 2,
                Math.Max(16, hoverDescriptionBounds.Height - DescriptionBoxInset.Y * 2 - bottomReservedWorld));
            Vector2 textPosition = Renderer.GetScreenPosition(worldInnerPosition);
            Vector2 textDimension = worldInnerDimension * Renderer.UIScaler.ToVector2() * Renderer.CurrentCamera.Zoom;
            string description = displayToken.Description ?? string.Empty;

            if (hoverDescriptionText == null)
            {
                hoverDescriptionText = new CustomText(
                    Game1.Instance,
                    "Fonts/DePixelHalbfett",
                    description,
                    textPosition,
                    textDimension,
                    padding: DescriptionTextPadding,
                    color: Color.White,
                    shadowColor: Color.Black * .45f)
                {
                    CenterText = true
                };
                hoverDescriptionText.Refresh();
            }
            else
            {
                hoverDescriptionText.Position = textPosition;
                if (hoverDescriptionText.Text != description ||
                    hoverDescriptionText.Dimension != textDimension)
                {
                    hoverDescriptionText.Text = description;
                    hoverDescriptionText.Dimension = textDimension;
                    hoverDescriptionText.Refresh();
                }
            }

            hoverDescriptionText.Color = Color.White * hoverDescriptionAlpha;
            hoverDescriptionText.ShadowColor = Color.Black * (.45f * hoverDescriptionAlpha);
            hoverDescriptionText.Update(elapsedSeconds);

            unlockButton.Position = Renderer.GetScreenPosition(hoverUnlockButtonBounds.Location.ToVector2());
            unlockButton.Color = Color.White * hoverDescriptionAlpha;
            if (showUnlockButton)
                unlockButton.StandardUpdate(gameTime);
        }

        public bool IsMouseOverHoverDescription(Vector2 worldMousePosition)
        {
            if (editing || hoverDescriptionTokenId < 0 || hoverDescriptionAlpha <= .1f)
                return false;

            Rectangle interactiveBounds = new Rectangle(
                hoverDescriptionBounds.X + 16,
                hoverDescriptionBounds.Y + 20,
                hoverDescriptionBounds.Width - 32,
                hoverDescriptionBounds.Height - 40);
            return interactiveBounds.Contains(worldMousePosition);
        }

        private void TryPurchaseToken(SkillTreeToken token)
        {
            if (token.IsCollected || !token.IsCollectable)
                return;

            if (token.ResourceCosts != null && token.ResourceCosts.Count > 0 && TrySpendResources != null)
            {
                Dictionary<string, int> costs = token.ResourceCosts
                    .Where(cost => !string.IsNullOrWhiteSpace(cost.Resource) && cost.Amount > 0)
                    .GroupBy(cost => cost.Resource)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
                if (!TrySpendResources(costs))
                    return;
            }

            UnlockToken(token.TokenID);
        }

        private void DrawHoverDescription(SpriteBatch sb)
        {
            if (editing || hoverDescriptionTokenId < 0 || hoverDescriptionAlpha <= .01f || hoverDescriptionText == null ||
                !treeTokens.TryGetValue(hoverDescriptionTokenId, out SkillTreeToken token))
                return;

            sb.Draw(ResourceAtlas.GetTexture("GUI_TextBox2"), hoverDescriptionBounds, null,
                Color.White * hoverDescriptionAlpha, 0f, Vector2.Zero, SpriteEffects.None, .94f);
            DrawHoverCostIcons(sb, token);
        }

        private void DrawHoverCostIcons(SpriteBatch sb, SkillTreeToken token)
        {
            if (token.ResourceCosts == null || token.ResourceCosts.Count == 0)
                return;

            SpriteFont font = ResourceAtlas.GetFont("DePixelHalbfett");
            Vector2 uiScale = Renderer.UIScaler.ToVector2() * Renderer.CurrentCamera.Zoom;
            int costY = (token.IsCollectable && !token.IsCollected)
                ? hoverUnlockButtonBounds.Y - CostIconWorldSize - 3
                : hoverDescriptionBounds.Bottom - DescriptionBoxInset.Y - CostIconWorldSize - 2;
            int spacing = 8;
            int totalWidth = 0;
            int[] itemWidths = new int[token.ResourceCosts.Count];
            for (int i = 0; i < token.ResourceCosts.Count; i++)
            {
                string amountText = token.ResourceCosts[i].Amount.ToString();
                int textWorldWidth = (int)MathF.Ceiling(font.MeasureString(amountText).X / Math.Max(1f, uiScale.X));
                itemWidths[i] = CostIconWorldSize + 2 + textWorldWidth;
                totalWidth += itemWidths[i] + (i > 0 ? spacing : 0);
            }

            int currentX = hoverDescriptionBounds.Center.X - totalWidth / 2;
            for (int i = 0; i < token.ResourceCosts.Count; i++)
            {
                ResourceCost cost = token.ResourceCosts[i];
                Texture2D icon = GetResourceIconTexture(cost.Resource);
                if (icon != null)
                {
                    Rectangle iconBounds = new Rectangle(currentX, costY, CostIconWorldSize, CostIconWorldSize);
                    sb.Draw(icon, iconBounds, null, GetResourceColor(cost.Resource) * hoverDescriptionAlpha, 0f, Vector2.Zero, SpriteEffects.None, .95f);
                }
                currentX += itemWidths[i] + spacing;
            }
        }

        private void DrawDescriptionUi(SpriteBatch sb)
        {
            if (editing || hoverDescriptionTokenId < 0 || hoverDescriptionAlpha <= .01f || hoverDescriptionText == null ||
                !treeTokens.TryGetValue(hoverDescriptionTokenId, out SkillTreeToken token))
                return;

            hoverDescriptionText.Draw();
            if (token.IsCollectable && !token.IsCollected)
                unlockButton.Draw(sb);
            DrawHoverCostAmountsUi(sb, token);
        }

        private void DrawHoverCostAmountsUi(SpriteBatch sb, SkillTreeToken token)
        {
            if (token.ResourceCosts == null || token.ResourceCosts.Count == 0)
                return;

            SpriteFont font = ResourceAtlas.GetFont("DePixelHalbfett");
            Vector2 uiScale = Renderer.UIScaler.ToVector2() * Renderer.CurrentCamera.Zoom;
            int costY = (token.IsCollectable && !token.IsCollected)
                ? hoverUnlockButtonBounds.Y - CostIconWorldSize - 3
                : hoverDescriptionBounds.Bottom - DescriptionBoxInset.Y - CostIconWorldSize - 2;
            int spacing = 8;
            int totalWidth = 0;
            int[] itemWidths = new int[token.ResourceCosts.Count];
            for (int i = 0; i < token.ResourceCosts.Count; i++)
            {
                string amountText = token.ResourceCosts[i].Amount.ToString();
                int textWorldWidth = (int)MathF.Ceiling(font.MeasureString(amountText).X / Math.Max(1f, uiScale.X));
                itemWidths[i] = CostIconWorldSize + 2 + textWorldWidth;
                totalWidth += itemWidths[i] + (i > 0 ? spacing : 0);
            }

            int currentX = hoverDescriptionBounds.Center.X - totalWidth / 2;
            for (int i = 0; i < token.ResourceCosts.Count; i++)
            {
                ResourceCost cost = token.ResourceCosts[i];
                string amountText = cost.Amount.ToString();
                Vector2 textMeasurement = font.MeasureString(amountText);
                Vector2 worldTextAnchor = new Vector2(currentX + CostIconWorldSize + 2, costY + CostIconWorldSize * .5f);
                Vector2 uiTextPos = Renderer.GetScreenPosition(worldTextAnchor) - new Vector2(0f, textMeasurement.Y * .5f);
                sb.DrawString(font, amountText, uiTextPos + new Vector2(-2f, 2f), Color.Black * (.45f * hoverDescriptionAlpha));
                sb.DrawString(font, amountText, uiTextPos, Color.White * hoverDescriptionAlpha);
                currentX += itemWidths[i] + spacing;
            }
        }

        private static Texture2D GetResourceIconTexture(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return null;

            string iconKey = resource.ToLowerInvariant() + "Icon";
            return ResourceAtlas.TextureCache != null && ResourceAtlas.TextureCache.ContainsKey(iconKey)
                ? ResourceAtlas.GetTexture(iconKey)
                : null;
        }

        private static Color GetResourceColor(string resource) => resource?.ToLowerInvariant() switch
        {
            "grass" => new Color(186, 180, 120),
            "cactus" => new Color(0, 255, 0),
            "flower" => new Color(255, 150, 255),
            "tree" => new Color(255, 212, 143),
            _ => Color.White
        };

        private static int GetPortalSize(SkillTreeToken token) => token.PortalSize > 0
            ? token.PortalSize
            : 32;

        public bool TryGetTokenAt(Vector2 mousePosition, out SkillTreeToken hitToken)
        {
            foreach (SkillTreeToken token in treeTokens.Values.OrderByDescending(token => token.Depth))
            {
                EnsureIconLayers(token);
                int hitboxSize = GetPortalSize(token);
                if (hitboxSize <= 0)
                    continue;

                Vector2 center = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
                float halfSize = hitboxSize * zoom / 2f;
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

        public void ControlledUpdate(GameTime gameTime)
        {
            unlockParticles.ControlledUpdate(gameTime);
            foreach (TokenUnlockWisp wisp in unlockWisps)
                wisp.ControlledUpdate(gameTime);
        }

        private ParticleSystem CreateUnlockFlashParticles(float size, int amt = 1)
        {
            return new ParticleSystem(new ParticleSystemStats
            {
                SpawnBounds = new[] { new[] { new Rectangle(0, 0, 1, 1) } },
                TrackPosition = () => unlockBurstPosition,
                TrackLayerDepth = () => .7f,
                ParticleLifeSpan = new[] { .15f },
                ParticleDespawnDistance = 1000f,
                MaxParticleCount = 16,
                ParticleStartColor = new[] { Color.White * .8f },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleTextureKeys = new[] { "star2" },
                ParticleSize = new[] { size },
                ParticleSpeed = new[] { 0f },
                StartingVelocity = new[] { Vector2.Zero },
                ActingForce = _ => Vector2.Zero,
                ParticleRotation = new[] { 0f, MathHelper.PiOver2 },
                ParticleRotationSpeed = _ => 0f,
                ParticleColorDecayRate = t => MathHelper.SmoothStep(0f, 1f, t),
                ParticleSizeDecayRate = t => (1f + .35f * t) * zoom,
                EmitRate = new[] { 0f },
                EmitCount = new[] { amt },
                ResetParticlesAfterDeath = false
            });
        }

        private ParticleSystem CreateUnlockWaveParticles()
        {
            float textureWidth = ResourceAtlas.GetTexture("wave").Width;
            return new ParticleSystem(new ParticleSystemStats
            {
                SpawnBounds = new[] { new[] { new Rectangle(0, 0, 1, 1) } },
                TrackPosition = () => unlockBurstPosition,
                TrackLayerDepth = () => .73f,
                ParticleLifeSpan = new[] { .65f },
                ParticleDespawnDistance = 1000f,
                MaxParticleCount = 16,
                ParticleStartColor = new[] { Color.White * .65f },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleTextureKeys = new[] { "wave1" },
                ParticleSize = new[] { 1f },
                ParticleSpeed = new[] { 0f },
                StartingVelocity = new[] { Vector2.Zero },
                ActingForce = _ => Vector2.Zero,
                ParticleRotation = new[] { 0f },
                ParticleRotationSpeed = _ => 0f,
                ParticleColorDecayRate = t => MathHelper.SmoothStep(0f, 1f, t),
                ParticleSizeDecayRate = t => /*(4f + 120f * (1f - MathF.Exp(-9f * t)))*/MathF.Sin(MathF.Pow(t,.333f)) * zoom,
                EmitRate = new[] { 0f },
                EmitCount = new[] { 1 },
                ResetParticlesAfterDeath = false
            });
        }

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
                TrackLayerDepth = () => .71f
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
                TrackLayerDepth = () => .72f,
                MaxParticleCount = 80,
                ParticleStartColor = new[] { Color.White },
                ParticleEndColor = new[] { Color.Transparent },
                ParticleTextureKeys = new[] { "square" },
                ParticleSpeed = new[] { .7f, 2.2f },
                ParticleSize = new[] { .05f, .1f },
                EmitRate = new[] { 0f },
                EmitCount = new[] { 28 },
                ParticleRotation = new[] { 0f, MathF.PI * 2 },
                ParticleRotationSpeed = _ => 0f,
                ParticleColorDecayRate = t => Math.Min(1f, t * 2f),
                ParticleSizeDecayRate = t => (1f - t) * zoom,
                StartingVelocityFromPosition = position =>
                {
                    Vector2 direction = position - unlockBurstPosition;
                    return direction.LengthSquared() > .0001f ? Vector2.Normalize(direction) * zoom : Vector2.UnitX * zoom;
                },
                ActingForce = _ => Vector2.Zero,
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
            Vector2 shakeDirection = RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One);
            if (shakeDirection.LengthSquared() > .0001f)
                Renderer.CurrentCamera?.ShakeCamera(32f, .65f, Vector2.Normalize(shakeDirection) * 80f);

            Shockwave?.Add(GetWorldPosition(token.GridPosition).ToVector2() * zoom);
            unlockBurstPosition = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
            unlockParticles.EmitParticles();
            Vector2 position = GetWorldPosition(token.GridPosition).ToVector2();
            float radius = Math.Max(GetPortalSize(token), token.IconLayers.Max(layer => Math.Max(layer.Width, layer.Height))) * .5f;
            unlockWisps.Add(new TokenUnlockWisp(position, radius));
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
            if (tokenPositions.TryGetValue(gridPosition, out SkillTreeToken token))
                UnlockToken(token.TokenID);
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

            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string projectDirectory = System.IO.Path.Combine(directory.FullName, "IdleCollector");
                if (System.IO.File.Exists(System.IO.Path.Combine(projectDirectory, "IdleCollector.csproj")))
                {
                    string path = System.IO.Path.Combine(projectDirectory, "Content", "SaveData", "SkillTree");
                    FileIO.WriteJsonTo(this, path, Newtonsoft.Json.Formatting.Indented);
                    return;
                }
                directory = directory.Parent;
            }
        }

        public void ResetSkillTree()
        {
            treeTokens = new();
            tokenPositions = new();
        }
    }
}
