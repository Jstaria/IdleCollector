using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SkillTreeCreationTool
{
    public class SkillTreeEditor : IScene
    {
        private const int IconCellSize = 16;
        private const int IconsPerRow = 10;
        private const int EffectHeaderHeight = 50;
        private const int EffectHeaderSpacing = 58;
        private const int EffectFieldHeight = 46;
        private const int EffectFieldSpacing = 8;

        private static readonly (EffectField Field, string Label)[] effectFields =
        {
            (EffectField.Key, "Key"),
            (EffectField.Value, "Value"),
            (EffectField.Amount, "Amount"),
            (EffectField.Description, "Desc")
        };

        public SkillTree skillTree { get; }

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private Action<SpriteBatch> drawFunction;
        private Action<GameTime> updateFunction;
        private readonly List<SkillTreeToken> parentTokens = new();
        private SkillTreeToken newToken;
        private readonly List<IconSelect> iconSelects = new();
        private readonly List<string> resourceOptions = new();
        private int scroll;
        private readonly HashSet<int> expandedEffects = new();
        private int effectScroll;
        private int activeCostIndex = -1;
        private CostField activeCostField;
        private string activeCostAmountText = string.Empty;
        private int resourceDropdownCostIndex = -1;
        private int activeEffectIndex = -1;
        private EffectField activeEffectField;
        private string activeAmountText = string.Empty;
        private bool editedTokenWasCollected;
        private bool unlockedInEditor;
        private bool createdTokenBeingEdited;
        private IconSizeField activeIconSizeField;
        private int activeIconSlot;
        private string iconSizeText = string.Empty;
        private bool replaceIconSizeText;

        private enum EffectField
        {
            None,
            Key,
            Value,
            Amount,
            Description
        }

        private enum IconSizeField
        {
            None,
            Width,
            Height
        }

        private enum CostField
        {
            None,
            Resource,
            Amount
        }

        public SkillTreeEditor(SkillTree st)
        {
            skillTree = st;
            drawFunction = DrawPreview;
            updateFunction = UpdatePreview;
            LoadIcons();
            LoadResourceOptions();
        }

        private void LoadResourceOptions()
        {
            JObject resourceData = JObject.Parse(System.IO.File.ReadAllText(FindResourceDataPath()));
            resourceOptions.AddRange(resourceData["resources"]!.Children<JProperty>()
                .Select(property => property.Name)
                .OrderBy(name => name));
        }

        private static string FindResourceDataPath()
        {
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string path = System.IO.Path.Combine(directory.FullName, "IdleCollector", "Content", "SaveData", "ResourceData.json");
                if (System.IO.File.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new System.IO.FileNotFoundException("Could not locate IdleCollector ResourceData.json.");
        }

        private void LoadIcons()
        {
            foreach (string icon in ResourceAtlas.TextureCache.Keys.OrderBy(key => key))
            {
                IconSelect iconSelect = new IconSelect(icon, ResourceAtlas.TextureCache[icon], new Point(IconCellSize));
                iconSelect.button.OnClick += () =>
                {
                    SetIconForActiveSlot(icon);

                    SetIconSizeForSlot(
                        activeIconSlot,
                        iconSelect.icon.Width,
                        iconSelect.icon.Height);

                    activeIconSizeField = IconSizeField.None;
                    iconSizeText = string.Empty;
                    replaceIconSizeText = false;
                };

                iconSelects.Add(iconSelect);
            }
        }

        public void Draw(SpriteBatch sb) => drawFunction?.Invoke(sb);
        public void DrawUi(SpriteBatch sb) { if (skillTree.editing) DrawEffectEditor(sb); }

        public void DrawPreview(SpriteBatch sb)
        {
            Point gridPosition = skillTree.GetGridPosition();
            Vector2 iconPosition = skillTree.GetWorldPosition(gridPosition).ToVector2() * skillTree.zoom;
            bool hasToken = skillTree.CheckForToken(gridPosition);
            Texture2D iconTexture = ResourceAtlas.GetTexture(skillTree.DefaultIcon);
            int iconWidth = skillTree.IconSize;
            int iconHeight = skillTree.IconSize;
            Vector2 iconSize = new Vector2(iconWidth, iconHeight) * skillTree.zoom;
            Rectangle iconRect = new Rectangle((iconPosition - iconSize / 2f).ToPoint(), iconSize.ToPoint());
            sb.Draw(iconTexture, iconRect, hasToken ? Color.Transparent : Color.Green * .75f);
            if (hasToken)
                sb.DrawCircleOutline(iconPosition, 3, 5, Color.Purple * .5f, .95f);
            foreach (SkillTreeToken token in parentTokens)
                sb.DrawCircleOutline(skillTree.GetWorldPosition(token.GridPosition).ToVector2() * skillTree.zoom, 3, 5, Color.Purple, .95f);

            foreach (SkillTreeToken token in parentTokens)
                sb.DrawLineCentered(iconPosition, skillTree.GetWorldPosition(token.GridPosition).ToVector2() * skillTree.zoom, 2, Color.White * .5f, .25f);
        }

        private void UpdatePreview(GameTime gameTime)
        {
            if (Input.IsButtonDownOnce(Keys.Delete) && parentTokens.Count > 0)
            {
                DeleteTokens(parentTokens);
                return;
            }

            if (Input.IsLeftButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();
                if (skillTree.CheckForToken(gPos))
                    ToggleParentToken(skillTree.GetToken(gPos));
                else
                    CreateToken(gPos);
            }

            if (Input.IsRightButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();
                if (skillTree.CheckForToken(gPos))
                    BeginEditing(skillTree.GetToken(gPos), gPos);
            }

            if (Input.IsMiddleButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();
                if (skillTree.CheckForToken(gPos))
                {
                    SkillTreeToken token = skillTree.GetToken(gPos);
                    if (Input.IsButtonDown(Keys.LeftControl) || Input.IsButtonDown(Keys.RightControl))
                        skillTree.UnlockToken(token.TokenID);
                    else
                        DeleteTokens(new[] { token });
                }
            }
        }

        private void DeleteTokens(IEnumerable<SkillTreeToken> tokens)
        {
            foreach (SkillTreeToken token in tokens.ToList())
                skillTree.RemoveToken(token.GridPosition);

            parentTokens.RemoveAll(token => !skillTree.CheckForToken(token.GridPosition));
        }

        private void ToggleParentToken(SkillTreeToken token)
        {
            if (!parentTokens.Remove(token))
                parentTokens.Add(token);
        }

        private void CreateToken(Point gridPosition)
        {
            newToken = skillTree.AddToken(gridPosition);
            foreach (SkillTreeToken parent in parentTokens)
                skillTree.SetTokenParent(parent, newToken);

            parentTokens.Clear();
            BeginEditing(newToken, gridPosition, true);
        }

        private void BeginEditing(SkillTreeToken token, Point gridPosition, bool wasCreated = false)
        {
            newToken = token;
            editedTokenWasCollected = token.IsCollected;
            unlockedInEditor = token.IsCollected;
            createdTokenBeingEdited = wasCreated;
            expandedEffects.Clear();
            effectScroll = 0;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
            activeCostIndex = -1;
            activeCostField = CostField.None;
            resourceDropdownCostIndex = -1;
            activeIconSizeField = IconSizeField.None;
            activeIconSlot = 0;
            iconSizeText = string.Empty;
            replaceIconSizeText = false;

            updateFunction = UpdateEdit;
            drawFunction = DrawEdit;
            skillTree.editing = true;

            Renderer.CurrentCamera.SetTarget(skillTree.GetWorldPosition(gridPosition));
            skillTree.zoomTarget = 1f;
        }

        public void SlowUpdate(GameTime gameTime) { }
        public void StandardUpdate(GameTime gameTime) => updateFunction?.Invoke(gameTime);
        public void ControlledUpdate(GameTime gameTime) { }

        private void UpdateEdit(GameTime gameTime)
        {
            UpdateIconSelect(gameTime);
            UpdateEffectEditor();
        }

        public void DrawEdit(SpriteBatch sb)
        {
            DrawIconSelect(sb);
        }

        private void DrawIconSelect(SpriteBatch sb)
        {
            LayoutIconSelects();
            for (int i = 0; i < iconSelects.Count; i++)
                iconSelects[i].Draw(sb);
        }

        private void LayoutIconSelects()
        {
            Point camPos = Renderer.CurrentCamera.Position;
            for (int i = 0; i < iconSelects.Count; i++)
            {
                float x = i % IconsPerRow * IconCellSize - camPos.X;
                float y = i / IconsPerRow * IconCellSize - camPos.Y - scroll;
                iconSelects[i].position = new Point((int)x, (int)y);
            }
        }

        private Rectangle GetIconSelectBounds()
        {
            if (iconSelects.Count == 0)
                return Rectangle.Empty;

            Rectangle bounds = new Rectangle(iconSelects[0].position, iconSelects[0].size);
            for (int i = 1; i < iconSelects.Count; i++)
                bounds = Rectangle.Union(bounds, new Rectangle(iconSelects[i].position, iconSelects[i].size));

            return bounds;
        }

        private void DrawEffectEditor(SpriteBatch sb)
        {
            if (newToken == null)
                return;

            Rectangle panel = GetEffectPanelBounds();
            Rectangle addCostButton = new Rectangle(panel.Right - 48, panel.Y + 66 - effectScroll, 38, 38);
            Rectangle unlockedBounds = GetUnlockedBounds(panel);
            Rectangle iconWidthBounds = GetIconWidthBounds(panel);
            Rectangle iconHeightBounds = GetIconHeightBounds(panel);
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");

            sb.Draw(Drawing.Pixel, panel, Color.Black * .8f);
            sb.DrawRect(panel, 1, Color.White * .4f);
            DrawIconSizeField(sb, font, iconWidthBounds, "W", GetIconWidthForSlot(activeIconSlot), IconSizeField.Width);
            DrawIconSizeField(sb, font, iconHeightBounds, "H", GetIconHeightForSlot(activeIconSlot), IconSizeField.Height);
            for (int i = 0; i < 3; i++)
                DrawIconSlot(sb, font, panel, i);

            sb.DrawString(font, "Textures", new Vector2(panel.X + 340, panel.Y + 50), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.DrawString(font, "Unlock", new Vector2(panel.X + 495, panel.Y + 6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, unlockedBounds, unlockedInEditor ? Color.ForestGreen : Color.Black * .6f);
            sb.DrawRect(unlockedBounds, 1, Color.White * (unlockedInEditor ? 1f : .25f));
            if (unlockedInEditor)
                sb.DrawString(font, "X", unlockedBounds.Location.ToVector2() + new Vector2(12, -3), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            int y = panel.Y + EffectHeaderSpacing - effectScroll;
            sb.Draw(Drawing.Pixel, new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight), Color.DarkSlateGray);
            sb.DrawString(font, "Costs", new Vector2(panel.X + 20, y + 2), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, addCostButton, Color.Green * .75f);
            sb.DrawString(font, "+", addCostButton.Location.ToVector2() + new Vector2(11, -6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            y += EffectHeaderSpacing;

            for (int i = 0; i < newToken.ResourceCosts.Count; i++)
            {
                ResourceCost cost = newToken.ResourceCosts[i];
                DrawRemoveButton(sb, font, GetCostRemoveBounds(panel, y));
                DrawCostField(sb, font, panel, ref y, i, CostField.Resource, "Resource", cost.Resource);
                DrawCostField(sb, font, panel, ref y, i, CostField.Amount, "Amount", cost.Amount.ToString(CultureInfo.InvariantCulture));
                y += 4;
            }

            Rectangle effectsHeader = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
            Rectangle addEffectButton = new Rectangle(effectsHeader.Right - 48, effectsHeader.Y + 6, 38, 38);
            sb.Draw(Drawing.Pixel, effectsHeader, Color.DarkSlateGray);
            sb.DrawString(font, "Effects", new Vector2(panel.X + 20, y + 2), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, addEffectButton, Color.Green * .75f);
            sb.DrawString(font, "+", addEffectButton.Location.ToVector2() + new Vector2(11, -6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            y += EffectHeaderSpacing;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                SkillEffectDefinition effect = newToken.Effects[i];
                Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
                bool expanded = expandedEffects.Contains(i);

                sb.Draw(Drawing.Pixel, header, expanded ? Color.DarkSlateGray : Color.Black * .6f);
                sb.DrawString(font, $"{(expanded ? "-" : "+")} Effect {i + 1}", header.Location.ToVector2() + new Vector2(10, 2), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
                DrawRemoveButton(sb, font, GetEffectRemoveBounds(header));
                y += EffectHeaderSpacing;

                if (!expanded)
                    continue;

                foreach ((EffectField field, string label) in effectFields)
                    DrawEffectField(sb, font, panel, ref y, i, field, label, GetEffectDisplayValue(effect, field));
                y += 4;
            }

            DrawResourceDropdown(sb, font, panel);
        }

        private static void DrawRemoveButton(SpriteBatch sb, SpriteFont font, Rectangle bounds)
        {
            sb.Draw(Drawing.Pixel, bounds, Color.DarkRed * .85f);
            sb.DrawRect(bounds, 1, Color.White * .35f);
            sb.DrawString(font, "-", bounds.Location.ToVector2() + new Vector2(11, -6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .98f);
        }

        private void DrawCostField(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y, int costIndex, CostField field, string label, string value)
        {
            Rectangle bounds = new Rectangle(panel.X + 165, y, panel.Width - 177, EffectFieldHeight);
            bool active = activeCostIndex == costIndex && activeCostField == field;
            sb.DrawString(font, label, new Vector2(panel.X + 14, y), Color.LightGray, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            sb.DrawString(font, value, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            y += EffectFieldHeight + EffectFieldSpacing;
        }

        private void DrawResourceDropdown(SpriteBatch sb, SpriteFont font, Rectangle panel)
        {
            if (resourceDropdownCostIndex < 0 || resourceDropdownCostIndex >= newToken.ResourceCosts.Count)
                return;

            Rectangle bounds = GetResourceDropdownBounds(panel, resourceDropdownCostIndex);
            sb.Draw(Drawing.Pixel, bounds, Color.Black * .95f);
            sb.DrawRect(bounds, 1, Color.White * .5f);
            for (int i = 0; i < resourceOptions.Count; i++)
            {
                Rectangle optionBounds = new Rectangle(bounds.X, bounds.Y + i * EffectFieldHeight, bounds.Width, EffectFieldHeight);
                bool selected = resourceOptions[i] == newToken.ResourceCosts[resourceDropdownCostIndex].Resource;
                sb.Draw(Drawing.Pixel, optionBounds, selected ? Color.DarkSlateGray : Color.Transparent);
                sb.DrawString(font, resourceOptions[i], optionBounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .98f);
            }
        }

        private Rectangle GetResourceDropdownBounds(Rectangle panel, int costIndex)
        {
            int costStartY = panel.Y + EffectHeaderSpacing - effectScroll + EffectHeaderSpacing;
            int rowHeight = (EffectFieldHeight + EffectFieldSpacing) * 2 + 4;
            int resourceY = costStartY + costIndex * rowHeight;
            return new Rectangle(panel.X + 165, resourceY + EffectFieldHeight, panel.Width - 177, resourceOptions.Count * EffectFieldHeight);
        }

        private static Rectangle GetCostRemoveBounds(Rectangle panel, int y) =>
            new Rectangle(panel.Right - 50, y, 38, 38);

        private static Rectangle GetEffectRemoveBounds(Rectangle header) =>
            new Rectangle(header.Right - 42, header.Y + 6, 34, 38);

        private void DrawEffectField(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y, int effectIndex, EffectField field, string label, string value)
        {
            int fieldHeight = GetEffectFieldHeight(font, panel, field, value);
            Rectangle fieldBounds = new Rectangle(panel.X + 165, y, panel.Width - 177, fieldHeight);
            bool active = activeEffectIndex == effectIndex && activeEffectField == field;

            sb.DrawString(font, label, new Vector2(panel.X + 14, y), Color.LightGray, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, fieldBounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(fieldBounds, 1, active ? Color.White : Color.White * .25f);
            string displayValue = field == EffectField.Description
                ? WrapText(font, value, fieldBounds.Width - 14)
                : value;
            sb.DrawString(font, displayValue, fieldBounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            y += fieldHeight + EffectFieldSpacing;
        }

        private static int GetEffectFieldHeight(SpriteFont font, Rectangle panel, EffectField field, string value)
        {
            if (field != EffectField.Description)
                return EffectFieldHeight;

            string wrappedValue = WrapText(font, value, panel.Width - 191);
            int lineCount = Math.Max(1, wrappedValue.Count(c => c == '\n') + 1);
            return Math.Max(80, lineCount * font.LineSpacing + 16);
        }

        private static string WrapText(SpriteFont font, string text, float maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            StringBuilder wrappedText = new StringBuilder();
            StringBuilder currentLine = new StringBuilder();
            foreach (char character in text)
            {
                if (character == '\r')
                    continue;

                if (character == '\n')
                {
                    wrappedText.Append(currentLine);
                    wrappedText.Append('\n');
                    currentLine.Clear();
                    continue;
                }

                string candidate = currentLine.ToString() + character;
                if (currentLine.Length > 0 && font.MeasureString(candidate).X > maxWidth)
                {
                    wrappedText.Append(currentLine);
                    wrappedText.Append('\n');
                    currentLine.Clear();

                    if (character == ' ')
                        continue;
                }

                currentLine.Append(character);
            }

            wrappedText.Append(currentLine);
            return wrappedText.ToString();
        }

        private static Rectangle GetEffectPanelBounds()
        {
            return new Rectangle(Renderer.UIBounds.Width - 728, 8, 720, Renderer.UIBounds.Height - 16);
        }

        private static Rectangle GetIconWidthBounds(Rectangle panel)
        {
            return new Rectangle(panel.X + 58, panel.Y + 8, 98, 42);
        }

        private static Rectangle GetIconHeightBounds(Rectangle panel)
        {
            return new Rectangle(panel.X + 218, panel.Y + 8, 98, 42);
        }

        private static Rectangle GetUnlockedBounds(Rectangle panel)
        {
            return new Rectangle(panel.X + 625, panel.Y + 8, 42, 42);
        }

        private static Rectangle GetIconSlotBounds(Rectangle panel, int slot)
        {
            return new Rectangle(panel.X + 340 + slot * 48, panel.Y + 8, 42, 42);
        }

        private string GetIconForSlot(int slot)
        {
            return slot switch
            {
                0 => newToken.TokenIcon,
                1 => newToken.TokenIcon2,
                2 => newToken.TokenIcon3,
                _ => string.Empty
            };
        }

        private int GetIconWidthForSlot(int slot)
        {
            return slot switch
            {
                0 => newToken.IconWidth,
                1 => newToken.IconWidth2,
                2 => newToken.IconWidth3,
                _ => 0
            };
        }

        private int GetIconHeightForSlot(int slot)
        {
            return slot switch
            {
                0 => newToken.IconHeight,
                1 => newToken.IconHeight2,
                2 => newToken.IconHeight3,
                _ => 0
            };
        }

        private void SetIconSizeForSlot(int slot, int width, int height)
        {
            switch (slot)
            {
                case 0:
                    newToken.IconWidth = width;
                    newToken.IconHeight = height;
                    break;

                case 1:
                    newToken.IconWidth2 = width;
                    newToken.IconHeight2 = height;
                    break;

                case 2:
                    newToken.IconWidth3 = width;
                    newToken.IconHeight3 = height;
                    break;
            }
        }

        private void SetIconWidthForSlot(int slot, int width)
        {
            switch (slot)
            {
                case 0:
                    newToken.IconWidth = width;
                    break;

                case 1:
                    newToken.IconWidth2 = width;
                    break;

                case 2:
                    newToken.IconWidth3 = width;
                    break;
            }
        }

        private void SetIconHeightForSlot(int slot, int height)
        {
            switch (slot)
            {
                case 0:
                    newToken.IconHeight = height;
                    break;

                case 1:
                    newToken.IconHeight2 = height;
                    break;

                case 2:
                    newToken.IconHeight3 = height;
                    break;
            }
        }

        private void SetIconForActiveSlot(string icon)
        {
            switch (activeIconSlot)
            {
                case 0:
                    newToken.TokenIcon = icon;
                    break;

                case 1:
                    newToken.TokenIcon2 = icon;
                    break;

                case 2:
                    newToken.TokenIcon3 = icon;
                    break;
            }
        }

        private void DrawIconSlot(SpriteBatch sb, SpriteFont font, Rectangle panel, int slot)
        {
            Rectangle bounds = GetIconSlotBounds(panel, slot);
            bool active = activeIconSlot == slot;

            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);

            string iconName = GetIconForSlot(slot);
            if (!string.IsNullOrEmpty(iconName))
            {
                Texture2D texture = ResourceAtlas.GetTexture(iconName);
                if (texture != null)
                {
                    Rectangle textureBounds = new Rectangle(
                        bounds.X + 3,
                        bounds.Y + 3,
                        bounds.Width - 6,
                        bounds.Height - 6);

                    sb.Draw(texture, textureBounds, Color.White);
                }
            }

            sb.DrawString(
                font,
                (slot + 1).ToString(),
                bounds.Location.ToVector2() + new Vector2(3, -3),
                active ? Color.Yellow : Color.White,
                0,
                Vector2.Zero,
                .65f,
                SpriteEffects.None,
                .98f);
        }

        private void DrawIconSizeField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, int value, IconSizeField field)
        {
            bool active = activeIconSizeField == field;
            sb.DrawString(font, label, new Vector2(bounds.X - 35, bounds.Y), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            string displayedValue = active ? iconSizeText : value.ToString(CultureInfo.InvariantCulture);
            sb.DrawString(font, displayedValue, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
        }

        private void UpdateEffectEditor()
        {
            if (newToken == null)
                return;

            Rectangle panel = GetEffectPanelBounds();
            Point mousePosition = (Input.GetMouseScreenPos().ToVector2() * Renderer.UIScaler.ToVector2()).ToPoint();
            bool mouseOverPanel = panel.Contains(mousePosition);
            int maximumScroll = GetMaximumEffectScroll(panel);

            if (mouseOverPanel)
                effectScroll = Math.Clamp(effectScroll - Input.GetMouseScrollDelta() * 36, 0, maximumScroll);
            else
                effectScroll = Math.Clamp(effectScroll, 0, maximumScroll);

            if (mouseOverPanel && Input.IsLeftButtonDownOnce())
                HandleEffectEditorClick(panel, mousePosition);

            UpdateEffectFieldText();
        }

        private int GetMaximumEffectScroll(Rectangle panel)
        {
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");
            int contentHeight = EffectHeaderSpacing;
            contentHeight += newToken.ResourceCosts.Count * (EffectFieldHeight + EffectFieldSpacing) * 2;
            contentHeight += newToken.ResourceCosts.Count * 4;
            contentHeight += EffectHeaderSpacing;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                contentHeight += EffectHeaderSpacing;
                if (!expandedEffects.Contains(i))
                    continue;

                SkillEffectDefinition effect = newToken.Effects[i];
                foreach ((EffectField field, _) in effectFields)
                    contentHeight += GetEffectFieldHeight(font, panel, field, GetEffectDisplayValue(effect, field)) + EffectFieldSpacing;
                contentHeight += 4;
            }

            int visibleHeight = panel.Height - 66;
            return Math.Max(0, contentHeight - visibleHeight);
        }

        private void HandleEffectEditorClick(Rectangle panel, Point mousePosition)
        {
            Rectangle addCostButton = new Rectangle(panel.Right - 48, panel.Y + 66 - effectScroll, 38, 38);
            if (TrySelectResourceDropdown(panel, mousePosition))
                return;

            for (int i = 0; i < 3; i++)
            {
                if (GetIconSlotBounds(panel, i).Contains(mousePosition))
                {
                    activeIconSlot = i;
                    activeIconSizeField = IconSizeField.None;
                    iconSizeText = string.Empty;
                    replaceIconSizeText = false;
                    return;
                }
            }

            if (GetUnlockedBounds(panel).Contains(mousePosition))
            {
                unlockedInEditor = !unlockedInEditor;
                return;
            }

            if (GetIconWidthBounds(panel).Contains(mousePosition))
            {
                SelectIconSizeField(IconSizeField.Width);
                return;
            }

            if (GetIconHeightBounds(panel).Contains(mousePosition))
            {
                SelectIconSizeField(IconSizeField.Height);
                return;
            }

            if (addCostButton.Contains(mousePosition))
            {
                skillTree.AddResourceCost(newToken.TokenID, resourceOptions.FirstOrDefault() ?? string.Empty, 0);
                SelectCostField(newToken.ResourceCosts.Count - 1, CostField.Resource);
                resourceDropdownCostIndex = newToken.ResourceCosts.Count - 1;
                return;
            }

            int y = panel.Y + EffectHeaderSpacing - effectScroll;
            y += EffectHeaderSpacing;
            for (int i = 0; i < newToken.ResourceCosts.Count; i++)
            {
                if (GetCostRemoveBounds(panel, y).Contains(mousePosition))
                {
                    RemoveCost(i);
                    return;
                }

                if (TrySelectCostField(panel, ref y, i, mousePosition, CostField.Resource) ||
                    TrySelectCostField(panel, ref y, i, mousePosition, CostField.Amount))
                    return;
                y += 4;
            }

            Rectangle effectsHeader = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
            Rectangle addEffectButton = new Rectangle(effectsHeader.Right - 48, effectsHeader.Y + 6, 38, 38);
            if (addEffectButton.Contains(mousePosition))
            {
                skillTree.AddEffect(newToken.TokenID, new SkillEffectDefinition());
                int index = newToken.Effects.Count - 1;
                expandedEffects.Add(index);
                activeIconSizeField = IconSizeField.None;
                SelectEffectField(index, EffectField.Key);
                return;
            }

            y += EffectHeaderSpacing;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
                if (GetEffectRemoveBounds(header).Contains(mousePosition))
                {
                    RemoveEffect(i);
                    return;
                }

                if (header.Contains(mousePosition))
                {
                    if (!expandedEffects.Add(i))
                        expandedEffects.Remove(i);
                    activeEffectIndex = -1;
                    activeEffectField = EffectField.None;
                    activeIconSizeField = IconSizeField.None;
                    return;
                }

                y += EffectHeaderSpacing;
                if (!expandedEffects.Contains(i))
                    continue;

                foreach ((EffectField field, _) in effectFields)
                    if (TrySelectEffectField(panel, ref y, i, mousePosition, field))
                        return;

                y += 4;
            }
        }

        private void RemoveCost(int index)
        {
            newToken.ResourceCosts.RemoveAt(index);
            activeCostIndex = -1;
            activeCostField = CostField.None;
            resourceDropdownCostIndex = -1;
        }

        private void RemoveEffect(int index)
        {
            newToken.Effects.RemoveAt(index);
            int[] expanded = expandedEffects.Where(effectIndex => effectIndex != index)
                .Select(effectIndex => effectIndex > index ? effectIndex - 1 : effectIndex)
                .ToArray();
            expandedEffects.Clear();
            foreach (int effectIndex in expanded)
                expandedEffects.Add(effectIndex);

            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
        }

        private bool TrySelectResourceDropdown(Rectangle panel, Point mousePosition)
        {
            if (resourceDropdownCostIndex < 0)
                return false;

            Rectangle bounds = GetResourceDropdownBounds(panel, resourceDropdownCostIndex);
            if (!bounds.Contains(mousePosition))
            {
                resourceDropdownCostIndex = -1;
                return false;
            }

            int optionIndex = (mousePosition.Y - bounds.Y) / EffectFieldHeight;
            if (optionIndex >= 0 && optionIndex < resourceOptions.Count)
                newToken.ResourceCosts[resourceDropdownCostIndex].Resource = resourceOptions[optionIndex];

            resourceDropdownCostIndex = -1;
            return true;
        }

        private bool TrySelectCostField(Rectangle panel, ref int y, int costIndex, Point mousePosition, CostField field)
        {
            Rectangle bounds = new Rectangle(panel.X + 165, y, panel.Width - 177, EffectFieldHeight);
            y += EffectFieldHeight + EffectFieldSpacing;
            if (!bounds.Contains(mousePosition))
                return false;

            if (field == CostField.Resource)
            {
                resourceDropdownCostIndex = costIndex;
                activeCostIndex = -1;
                activeCostField = CostField.None;
            }
            else
            {
                resourceDropdownCostIndex = -1;
                SelectCostField(costIndex, field);
            }
            return true;
        }

        private bool TrySelectEffectField(Rectangle panel, ref int y, int effectIndex, Point mousePosition, EffectField field)
        {
            string value = GetEffectDisplayValue(newToken.Effects[effectIndex], field);
            int fieldHeight = GetEffectFieldHeight(ResourceAtlas.GetFont("EffectEditor"), panel, field, value);
            Rectangle bounds = new Rectangle(panel.X + 165, y, panel.Width - 177, fieldHeight);
            y += fieldHeight + EffectFieldSpacing;
            if (!bounds.Contains(mousePosition))
                return false;

            SelectEffectField(effectIndex, field);
            return true;
        }

        private void SelectEffectField(int effectIndex, EffectField field)
        {
            activeEffectIndex = effectIndex;
            activeEffectField = field;
            activeCostIndex = -1;
            activeCostField = CostField.None;
            activeIconSizeField = IconSizeField.None;
            activeAmountText = field == EffectField.Amount
                ? newToken.Effects[effectIndex].Amount.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private void UpdateEffectFieldText()
        {
            string typedText = Game1.ConsumeTextInput();
            if (activeIconSizeField != IconSizeField.None)
            {
                UpdateIconSize(typedText);
                return;
            }

            if (activeCostIndex >= 0 && activeCostIndex < newToken.ResourceCosts.Count)
            {
                UpdateCostFieldText(typedText);
                return;
            }

            if (activeEffectIndex < 0 || activeEffectIndex >= newToken.Effects.Count)
                return;

            SkillEffectDefinition effect = newToken.Effects[activeEffectIndex];
            string value = activeEffectField == EffectField.Amount
                ? activeAmountText
                : GetEffectDisplayValue(effect, activeEffectField);
            foreach (char character in typedText)
            {
                int maximumLength = activeEffectField == EffectField.Description ? 240 : 80;
                if (value.Length < maximumLength)
                    value += character;
            }

            if (Input.IsButtonDownOnce(Keys.Back) && value.Length > 0)
                value = value[..^1];

            SetEffectFieldValue(effect, activeEffectField, value);
        }

        private void SelectCostField(int costIndex, CostField field)
        {
            activeCostIndex = costIndex;
            activeCostField = field;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
            activeIconSizeField = IconSizeField.None;
            activeCostAmountText = field == CostField.Amount
                ? newToken.ResourceCosts[costIndex].Amount.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private void UpdateCostFieldText(string typedText)
        {
            ResourceCost cost = newToken.ResourceCosts[activeCostIndex];
            string value = activeCostField == CostField.Amount ? activeCostAmountText : cost.Resource;
            foreach (char character in typedText)
            {
                bool valid = activeCostField == CostField.Amount ? char.IsDigit(character) : !char.IsControl(character);
                if (valid && value.Length < 40)
                    value += character;
            }

            if (Input.IsButtonDownOnce(Keys.Back) && value.Length > 0)
                value = value[..^1];

            if (activeCostField == CostField.Resource)
                cost.Resource = value;
            else
            {
                activeCostAmountText = value;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                    cost.Amount = Math.Max(0, amount);
            }
        }

        private void UpdateIconSize(string typedText)
        {
            foreach (char character in typedText)
                if (char.IsDigit(character))
                {
                    if (replaceIconSizeText)
                    {
                        iconSizeText = string.Empty;
                        replaceIconSizeText = false;
                    }

                    if (iconSizeText.Length < 4)
                        iconSizeText += character;
                }

            if (Input.IsButtonDownOnce(Keys.Back) && iconSizeText.Length > 0)
            {
                replaceIconSizeText = false;
                iconSizeText = iconSizeText[..^1];
            }

            if (!int.TryParse(iconSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                return;

            if (activeIconSizeField == IconSizeField.Width)
                SetIconWidthForSlot(activeIconSlot, Math.Max(1, size));
            else
                SetIconHeightForSlot(activeIconSlot, Math.Max(1, size));
        }

        private void SelectIconSizeField(IconSizeField field)
        {
            activeIconSizeField = field;
            iconSizeText = (field == IconSizeField.Width
                ? GetIconWidthForSlot(activeIconSlot)
                : GetIconHeightForSlot(activeIconSlot))
                .ToString(CultureInfo.InvariantCulture);
            replaceIconSizeText = true;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
        }

        private static string GetEffectDisplayValue(SkillEffectDefinition effect, EffectField field)
        {
            return field switch
            {
                EffectField.Key => effect.Key,
                EffectField.Value => effect.Value,
                EffectField.Amount => effect.Amount.ToString(CultureInfo.InvariantCulture),
                EffectField.Description => effect.SkillEffectDescription,
                _ => string.Empty
            };
        }

        private void SetEffectFieldValue(SkillEffectDefinition effect, EffectField field, string value)
        {
            switch (field)
            {
                case EffectField.Key:
                    effect.Key = value;
                    break;
                case EffectField.Value:
                    effect.Value = value;
                    break;
                case EffectField.Amount:
                    activeAmountText = value;
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                        effect.Amount = amount;
                    break;
                case EffectField.Description:
                    effect.SkillEffectDescription = value;
                    break;
            }
        }

        private void UpdateIconSelect(GameTime gt)
        {
            LayoutIconSelects();
            if (GetIconSelectBounds().Contains(Input.GetMousePos()))
            {
                if (!(GetIconSelectBounds().Height < Renderer.RenderSize.Y))
                {
                    scroll = Math.Clamp(scroll + Input.GetMouseScrollDelta() * 20, 0, 100);
                    LayoutIconSelects();
                }
            }

            for (int i = 0; i < iconSelects.Count; i++)
            {
                IconSelect icon = iconSelects[i];

                icon.Update(gt);
            }

            if (Input.IsButtonDownOnce(Keys.Escape))
                CancelEditing();
            else if (Input.IsButtonDownOnce(Keys.Enter))
                EndEditing();
        }

        private void EndEditing(bool save = true)
        {
            updateFunction = UpdatePreview;
            drawFunction = DrawPreview;
            skillTree.editing = false;
            if (save && unlockedInEditor && !newToken.IsCollected)
                skillTree.UnlockToken(newToken.TokenID);
            else
                newToken.IsCollected = save ? unlockedInEditor : editedTokenWasCollected;
            createdTokenBeingEdited = false;
        }

        private void CancelEditing()
        {
            if (createdTokenBeingEdited)
                skillTree.RemoveToken(newToken.GridPosition);

            EndEditing(false);
        }
    }
}
