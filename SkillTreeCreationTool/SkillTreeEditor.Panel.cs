using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private readonly SkillTreeEditorHeaderDropdown costsHeaderDropdown = new("Costs");
        private readonly SkillTreeEditorHeaderDropdown effectsHeaderDropdown = new("Effects");
        private readonly List<SkillTreeEditorHeaderDropdown> effectHeaderDropdowns = new();

        private SkillTreeEditorHeaderDropdown GetEffectHeaderDropdown(int index)
        {
            while (effectHeaderDropdowns.Count <= index)
                effectHeaderDropdowns.Add(new SkillTreeEditorHeaderDropdown(string.Empty));

            SkillTreeEditorHeaderDropdown header = effectHeaderDropdowns[index];
            header.Label = $"Effect {index + 1}";
            return header;
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

        private void DrawEffectEditor(SpriteBatch sb)
        {
            if (newToken == null)
                return;

            Rectangle panel = GetEffectPanelBounds();
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");
            sb.Draw(Drawing.Pixel, panel, Color.Black * .8f);
            sb.DrawRect(panel, 1, Color.White * .4f);

            DrawIconSizeField(sb, font, GetPortalSizeBounds(panel), "Portal Size", newToken.PortalSize, IconSizeField.Portal, new Point(-185, 0));
            DrawUnlockedField(sb, font, panel);

            int y = panel.Y + EffectHeaderSpacing - effectScroll;
            DrawCosts(sb, font, panel, ref y);
            DrawEffects(sb, font, panel, ref y);
            DrawIconLayerSection(sb, font, panel, ref y);
            DrawResourceDropdown(sb, font, panel);
        }

        private void DrawUnlockedField(SpriteBatch sb, SpriteFont font, Rectangle panel)
        {
            Rectangle bounds = GetUnlockedBounds(panel);
            sb.DrawString(font, "Unlock", new Vector2(panel.X + 495, panel.Y + 6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, unlockedInEditor ? Color.ForestGreen : Color.Black * .6f);
            sb.DrawRect(bounds, 1, Color.White * (unlockedInEditor ? 1f : .25f));
            if (unlockedInEditor)
                sb.DrawString(font, "X", bounds.Location.ToVector2() + new Vector2(12, -3), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
        }

        private void DrawCosts(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y)
        {
            Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
            costsHeaderDropdown.IsExpanded = true;
            costsHeaderDropdown.Layout(header, false, true);
            costsHeaderDropdown.Draw(sb, font);
            y += EffectHeaderSpacing;

            for (int i = 0; i < newToken.ResourceCosts.Count; i++)
            {
                ResourceCost cost = newToken.ResourceCosts[i];
                DrawRemoveButton(sb, font, GetCostRemoveBounds(panel, y));
                DrawCostField(sb, font, panel, ref y, i, CostField.Resource, "Resource", cost.Resource);
                DrawCostField(sb, font, panel, ref y, i, CostField.Amount, "Amount", cost.Amount.ToString(CultureInfo.InvariantCulture));
                y += 4;
            }
        }

        private void DrawEffects(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y)
        {
            Rectangle header = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
            effectsHeaderDropdown.IsExpanded = true;
            effectsHeaderDropdown.Layout(header, false, true);
            effectsHeaderDropdown.Draw(sb, font);
            y += EffectHeaderSpacing;

            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                SkillEffectDefinition effect = newToken.Effects[i];
                Rectangle effectHeader = new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);
                bool expanded = expandedEffects.Contains(i);
                SkillTreeEditorHeaderDropdown effectHeaderDropdown = GetEffectHeaderDropdown(i);
                effectHeaderDropdown.IsExpanded = expanded;
                effectHeaderDropdown.Layout(effectHeader, true, false);
                effectHeaderDropdown.Draw(sb, font);
                DrawRemoveButton(sb, font, GetEffectRemoveBounds(effectHeader));
                y += EffectHeaderSpacing;
                if (!expanded)
                    continue;

                foreach ((EffectField field, string label) in effectFields)
                    DrawEffectField(sb, font, panel, ref y, i, field, label, GetEffectDisplayValue(effect, field));
                y += 4;
            }
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

        private static Rectangle GetCostRemoveBounds(Rectangle panel, int y) => new Rectangle(panel.Right - 50, y, 38, 38);
        private static Rectangle GetEffectRemoveBounds(Rectangle header) => new Rectangle(header.Right - 42, header.Y + 6, 34, 38);

        private void DrawEffectField(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y, int effectIndex, EffectField field, string label, string value)
        {
            int fieldHeight = GetEffectFieldHeight(font, panel, field, value);
            Rectangle bounds = new Rectangle(panel.X + 165, y, panel.Width - 177, fieldHeight);
            bool active = activeEffectIndex == effectIndex && activeEffectField == field;
            sb.DrawString(font, label, new Vector2(panel.X + 14, y), Color.LightGray, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            string displayValue = field == EffectField.Description ? WrapText(font, value, bounds.Width - 14) : value;
            sb.DrawString(font, displayValue, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
            y += fieldHeight + EffectFieldSpacing;
        }

        private static int GetEffectFieldHeight(SpriteFont font, Rectangle panel, EffectField field, string value)
        {
            if (field != EffectField.Description)
                return EffectFieldHeight;

            string wrappedValue = WrapText(font, value, panel.Width - 191);
            int lines = Math.Max(1, wrappedValue.Count(character => character == '\n') + 1);
            return Math.Max(80, lines * font.LineSpacing + 16);
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
                    wrappedText.AppendLine(currentLine.ToString());
                    currentLine.Clear();
                    continue;
                }

                if (currentLine.Length > 0 && font.MeasureString(currentLine.ToString() + character).X > maxWidth)
                {
                    wrappedText.AppendLine(currentLine.ToString());
                    currentLine.Clear();
                    if (character == ' ')
                        continue;
                }
                currentLine.Append(character);
            }

            return wrappedText.Append(currentLine).ToString();
        }

        private static Rectangle GetEffectPanelBounds() => new Rectangle(Renderer.UIBounds.Width - 728, 8, 720, Renderer.UIBounds.Height - 16);
        private static Rectangle GetPortalSizeBounds(Rectangle panel) => new Rectangle(panel.X + 340, panel.Y + 8, 98, 42);
        private static Rectangle GetUnlockedBounds(Rectangle panel) => new Rectangle(panel.X + 625, panel.Y + 8, 42, 42);

        private void DrawIconSizeField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, int value, IconSizeField field, Point textOffset = default)
        {
            bool active = activeIconSizeField == field;
            sb.DrawString(font, label, new Vector2(bounds.X - 35 + textOffset.X, bounds.Y + textOffset.Y), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            string displayedValue = active ? iconSizeText : value.ToString(CultureInfo.InvariantCulture);
            sb.DrawString(font, displayedValue, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
        }
    }
}
