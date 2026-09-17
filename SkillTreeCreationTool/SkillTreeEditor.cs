using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTreeEditor : IScene
    {
        public SkillTree skillTree;

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private delegate void DrawFunc(SpriteBatch sb);
        private DrawFunc DrawFunction;

        private delegate void UpdateFunc(GameTime sb);
        private UpdateFunc UpdateFunction;

        private List<SkillTreeToken> parentTokens;
        private SkillTreeToken newToken;
        private List<IconSelect> iconSelects;
        private int scroll = 0;
        private readonly HashSet<int> expandedEffects = new();
        private int effectScroll;
        private int activeEffectIndex = -1;
        private EffectField activeEffectField;
        private string activeAmountText = string.Empty;
        private bool editedTokenWasCollected;
        private IconSizeField activeIconSizeField;
        private string iconSizeText = string.Empty;

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

        public SkillTreeEditor(SkillTree st)
        {
            parentTokens = new();

            skillTree = st;
            DrawFunction = DrawPreview;
            UpdateFunction = UpdatePreview;

            LoadIcons();
        }

        private void LoadIcons()
        {
            iconSelects = new List<IconSelect>();

            var textures = ResourceAtlas.TextureCache;
            var textureKeys = textures.Keys.ToList().OrderBy(k => k);

            foreach (var icon in textureKeys)
            {
                IconSelect iconSelect = new IconSelect(icon, textures[icon], new Point(32));
                iconSelect.button.OnClick += () =>
                {
                    newToken.TokenIcon = icon;
                    newToken.IconWidth = iconSelect.icon.Width;
                    newToken.IconHeight = iconSelect.icon.Height;
                    activeIconSizeField = IconSizeField.None;
                    iconSizeText = string.Empty;
                };

                iconSelects.Add(iconSelect);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            DrawFunction?.Invoke(sb);
        }

        public void DrawUi(SpriteBatch sb)
        {
            if (skillTree.editing)
                DrawEffectEditor(sb);
        }

        public void DrawPreview(SpriteBatch sb)
        {
            Point gridPosition = skillTree.GetGridPosition();
            Vector2 iconPosition = skillTree.GetWorldPosition(gridPosition).ToVector2() * skillTree.zoom;
            bool hasToken = skillTree.CheckForToken(gridPosition);
            Texture2D iconTexture = ResourceAtlas.GetTexture(newToken?.TokenIcon ?? skillTree.DefaultIcon);
            int iconWidth = newToken?.IconWidth ?? skillTree.IconSize;
            int iconHeight = newToken?.IconHeight ?? skillTree.IconSize;
            Vector2 iconSize = new Vector2(iconWidth, iconHeight) * skillTree.zoom;
            Rectangle iconRect = new Rectangle((iconPosition - iconSize / 2f).ToPoint(), iconSize.ToPoint());
            sb.Draw(iconTexture, iconRect, hasToken ? Color.Transparent : Color.Green * .75f);
            if (hasToken)
                sb.DrawCircleOutline(iconPosition, 3, 5, Color.Purple * .5f, .95f);
            foreach (var token in parentTokens)
            {
                sb.DrawCircleOutline(skillTree.GetWorldPosition(token.GridPosition).ToVector2() * skillTree.zoom, 3, 5, Color.Purple, .95f);
            }
            if (parentTokens.Count > 0)
            {
                for (int j = 0; j < parentTokens.Count; j++)
                {
                    Vector2 parentPos = skillTree.GetWorldPosition(parentTokens[j].GridPosition).ToVector2() * skillTree.zoom;
                    sb.DrawLineCentered(
                        skillTree.GetWorldPosition(
                            skillTree.GetGridPosition(Input.GetMousePos().ToVector2())).ToVector2() * skillTree.zoom,
                        parentPos, 2, Color.White * .5f, .25f);
                }
            }
        }

        private void UpdatePreview(GameTime gameTime)
        {
            if (Input.IsLeftButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();

                if (skillTree.CheckForToken(gPos))
                {
                    SkillTreeToken token = skillTree.GetToken(gPos);

                    if (parentTokens.Contains(token)) parentTokens.Remove(token);
                    else parentTokens.Add(token);
                }
                else
                {
                    newToken = skillTree.AddToken(gPos);

                    foreach (SkillTreeToken token in parentTokens)
                    {
                        skillTree.SetTokenParent(token, newToken);
                    }

                    parentTokens.Clear();

                    BeginEditing(newToken, gPos);
                }

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
                if (!skillTree.CheckForToken(gPos)) return;

                var token = skillTree.GetToken(gPos);

                if (parentTokens.Contains(token)) parentTokens.Remove(token);

                skillTree.RemoveToken(gPos);
            }
        }

        private void BeginEditing(SkillTreeToken token, Point gridPosition)
        {
            newToken = token;
            editedTokenWasCollected = token.IsCollected;
            expandedEffects.Clear();
            effectScroll = 0;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
            activeIconSizeField = IconSizeField.None;
            iconSizeText = string.Empty;

            UpdateFunction = UpdateEdit;
            DrawFunction = DrawEdit;
            skillTree.editing = true;

            Renderer.CurrentCamera.SetTarget(skillTree.GetWorldPosition(gridPosition));
            skillTree.zoomTarget = 1f;
            newToken.IsCollected = true;
        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }

        public void StandardUpdate(GameTime gameTime)
        {
            UpdateFunction?.Invoke(gameTime);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
        }

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
                float x = i % 5 * 32 - camPos.X;
                float y = i / 5 * 32 - camPos.Y - scroll;
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
            Rectangle addButton = new Rectangle(panel.Right - 48, panel.Y + 10, 38, 38);
            Rectangle iconWidthBounds = GetIconWidthBounds(panel);
            Rectangle iconHeightBounds = GetIconHeightBounds(panel);
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");

            sb.Draw(Drawing.Pixel, panel, Color.Black * .8f);
            sb.DrawRect(panel, 1, Color.White * .4f);
            DrawIconSizeField(sb, font, iconWidthBounds, "W", newToken.IconWidth, IconSizeField.Width);
            DrawIconSizeField(sb, font, iconHeightBounds, "H", newToken.IconHeight, IconSizeField.Height);
            sb.DrawString(font, "Effects", new Vector2(panel.X + 350, panel.Y + 6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, addButton, Color.Green * .75f);
            sb.DrawString(font, "+", addButton.Location.ToVector2() + new Vector2(11, -6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);

            int y = panel.Y + 58 - effectScroll;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                SkillEffectDefinition effect = newToken.Effects[i];
                Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, 50);
                bool expanded = expandedEffects.Contains(i);

                sb.Draw(Drawing.Pixel, header, expanded ? Color.DarkSlateGray : Color.Black * .6f);
                sb.DrawString(font, $"{(expanded ? "-" : "+")} Effect {i + 1}", header.Location.ToVector2() + new Vector2(10, 2), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
                y += 58;

                if (!expanded)
                    continue;

                DrawEffectField(sb, font, panel, ref y, i, EffectField.Key, "Key", effect.Key);
                DrawEffectField(sb, font, panel, ref y, i, EffectField.Value, "Value", effect.Value);
                DrawEffectField(sb, font, panel, ref y, i, EffectField.Amount, "Amount", effect.Amount.ToString(CultureInfo.InvariantCulture));
                DrawEffectField(sb, font, panel, ref y, i, EffectField.Description, "Desc", effect.SkillEffectDescription);
                y += 4;
            }
        }

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
            y += fieldHeight + 8;
        }

        private static int GetEffectFieldHeight(SpriteFont font, Rectangle panel, EffectField field, string value)
        {
            if (field != EffectField.Description)
                return 46;

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

        private void DrawIconSizeField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, int value, IconSizeField field)
        {
            bool active = activeIconSizeField == field;
            sb.DrawString(font, label, new Vector2(bounds.X - 35, bounds.Y), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            string displayedValue = active ? iconSizeText : value.ToString(CultureInfo.InvariantCulture);
            sb.DrawString(font, displayedValue, bounds.Location.ToVector2() + new Vector2(7,0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
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
            int contentHeight = 0;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                contentHeight += 58;
                if (!expandedEffects.Contains(i))
                    continue;

                SkillEffectDefinition effect = newToken.Effects[i];
                contentHeight += GetEffectFieldHeight(font, panel, EffectField.Key, effect.Key) + 8;
                contentHeight += GetEffectFieldHeight(font, panel, EffectField.Value, effect.Value) + 8;
                contentHeight += GetEffectFieldHeight(font, panel, EffectField.Amount, effect.Amount.ToString(CultureInfo.InvariantCulture)) + 8;
                contentHeight += GetEffectFieldHeight(font, panel, EffectField.Description, effect.SkillEffectDescription) + 12;
            }

            int visibleHeight = panel.Height - 66;
            return Math.Max(0, contentHeight - visibleHeight);
        }

        private void HandleEffectEditorClick(Rectangle panel, Point mousePosition)
        {
            Rectangle addButton = new Rectangle(panel.Right - 48, panel.Y + 10, 38, 38);
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

            if (addButton.Contains(mousePosition))
            {
                skillTree.AddEffect(newToken.TokenID, new SkillEffectDefinition());
                int index = newToken.Effects.Count - 1;
                expandedEffects.Add(index);
                activeIconSizeField = IconSizeField.None;
                SelectEffectField(index, EffectField.Key);
                return;
            }

            int y = panel.Y + 58 - effectScroll;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, 50);
                if (header.Contains(mousePosition))
                {
                    if (!expandedEffects.Add(i))
                        expandedEffects.Remove(i);
                    activeEffectIndex = -1;
                    activeEffectField = EffectField.None;
                    activeIconSizeField = IconSizeField.None;
                    return;
                }

                y += 58;
                if (!expandedEffects.Contains(i))
                    continue;

                if (TrySelectEffectField(panel, ref y, i, mousePosition, EffectField.Key) ||
                    TrySelectEffectField(panel, ref y, i, mousePosition, EffectField.Value) ||
                    TrySelectEffectField(panel, ref y, i, mousePosition, EffectField.Amount) ||
                    TrySelectEffectField(panel, ref y, i, mousePosition, EffectField.Description))
                    return;

                y += 4;
            }
        }

        private bool TrySelectEffectField(Rectangle panel, ref int y, int effectIndex, Point mousePosition, EffectField field)
        {
            string value = GetEffectFieldValue(newToken.Effects[effectIndex], field);
            int fieldHeight = GetEffectFieldHeight(ResourceAtlas.GetFont("EffectEditor"), panel, field, value);
            Rectangle bounds = new Rectangle(panel.X + 165, y, panel.Width - 177, fieldHeight);
            y += fieldHeight + 8;
            if (!bounds.Contains(mousePosition))
                return false;

            SelectEffectField(effectIndex, field);
            return true;
        }

        private void SelectEffectField(int effectIndex, EffectField field)
        {
            activeEffectIndex = effectIndex;
            activeEffectField = field;
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
                foreach (char character in typedText)
                {
                    if (iconSizeText.Length < 4 && char.IsDigit(character))
                        iconSizeText += character;
                }

                if (Input.IsButtonDownOnce(Keys.Back) && iconSizeText.Length > 0)
                    iconSizeText = iconSizeText[..^1];

                if (int.TryParse(iconSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iconSize))
                {
                    if (activeIconSizeField == IconSizeField.Width)
                        newToken.IconWidth = Math.Max(1, iconSize);
                    else
                        newToken.IconHeight = Math.Max(1, iconSize);
                }

                return;
            }

            if (activeEffectIndex < 0 || activeEffectIndex >= newToken.Effects.Count)
                return;

            SkillEffectDefinition effect = newToken.Effects[activeEffectIndex];
            string value = GetEffectFieldValue(effect, activeEffectField);
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

        private void SelectIconSizeField(IconSizeField field)
        {
            activeIconSizeField = field;
            iconSizeText = (field == IconSizeField.Width ? newToken.IconWidth : newToken.IconHeight)
                .ToString(CultureInfo.InvariantCulture);
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
        }

        private string GetEffectFieldValue(SkillEffectDefinition effect, EffectField field)
        {
            return field switch
            {
                EffectField.Key => effect.Key,
                EffectField.Value => effect.Value,
                EffectField.Amount => activeAmountText,
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
                scroll = Math.Clamp(scroll + Input.GetMouseScrollDelta() * 20, 0, 100);
                LayoutIconSelects();
            }

            for (int i = 0; i < iconSelects.Count; i++)
            {
                IconSelect icon = iconSelects[i];

                icon.Update(gt);
            }

            if (Input.IsButtonDownOnce(Keys.Enter))
            {
                UpdateFunction = UpdatePreview;
                DrawFunction = DrawPreview;

                skillTree.editing = false;
                newToken.IsCollected = editedTokenWasCollected;
            }
        }
    }
}
