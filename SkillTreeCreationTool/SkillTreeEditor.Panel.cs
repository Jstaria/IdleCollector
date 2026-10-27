using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private void LoadResourceOptions()
        {
            JObject resourceData = JObject.Parse(System.IO.File.ReadAllText(FindResourceDataPath()));
            resourceOptions.AddRange(resourceData["resources"]!.Children<JProperty>()
                .Select(property => property.Name).OrderBy(name => name));
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

            PanelLayout layout = GetPanelLayout();
            Rectangle panel = layout.Bounds;
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");
            sb.Draw(Drawing.Pixel, panel, Color.Black * .8f);
            sb.DrawRect(panel, 1, Color.White * .4f);

            Rectangle portal = GetPortalSizeBounds(panel);
            bool active = activeIconSizeField == IconSizeField.Portal;
            DrawField(sb, font, portal, "Portal Size", active ? iconSizeText : newToken.PortalSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                active, new Vector2(portal.X - 220, portal.Y), Color.White);
            DrawUnlockedField(sb, font, panel);

            foreach (PanelRow row in layout.Rows)
                DrawPanelRow(sb, font, panel, row);
            DrawResourceDropdown(sb, font, layout);
            DrawConstellationStarDropdown(sb, font, layout);
        }

        private void DrawPanelRow(SpriteBatch sb, SpriteFont font, Rectangle panel, PanelRow row)
        {
            switch (row.Kind)
            {
                case PanelRowKind.Description:
                    DrawField(sb, font, row.Bounds, row.Label, row.Value, editingTokenDescription,
                        new Vector2(row.Bounds.X + 4, row.Bounds.Y - font.LineSpacing), Color.LightGray);
                    return;
                case PanelRowKind.ConstellationStar:
                    headerDropdown.Draw(sb, font, row.Bounds, row.Label, constellationStarDropdownOpen, expandable: true);
                    return;
                case PanelRowKind.Costs:
                case PanelRowKind.Effects:
                case PanelRowKind.Layers:
                    headerDropdown.Draw(sb, font, row.Bounds, row.Label, addButton: true);
                    return;
                case PanelRowKind.Effect:
                    headerDropdown.Draw(sb, font, row.Bounds, row.Label, expandedEffects.Contains(row.Index), expandable: true);
                    SkillTreeEditorHeaderDropdown.DrawRemoveButton(sb, font, SkillTreeEditorHeaderDropdown.GetRemoveButtonBounds(row.Bounds));
                    return;
                case PanelRowKind.Layer:
                    DrawLayerRow(sb, font, row);
                    return;
                case PanelRowKind.LayerTint:
                    DrawLayerTint(sb, font, row);
                    return;
            }

            if (row.Kind == PanelRowKind.Resource)
                SkillTreeEditorHeaderDropdown.DrawRemoveButton(sb, font, GetCostRemoveBounds(row.Bounds));
            DrawField(sb, font, row.Bounds, row.Label, row.Value, IsPanelRowSelected(row),
                new Vector2(panel.X + 14, row.Bounds.Y), Color.LightGray);
        }

        private bool IsPanelRowSelected(PanelRow row)
        {
            if (row.Kind == PanelRowKind.EffectField)
                return activeEffectIndex == row.Index && activeEffectField == row.Field;

            CostField field = row.Kind == PanelRowKind.Resource ? CostField.Resource : CostField.Amount;
            return activeCostIndex == row.Index && activeCostField == field;
        }

        private static void DrawField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, string value,
            bool active, Vector2 labelPosition, Color labelColor)
        {
            sb.DrawString(font, label, labelPosition, labelColor, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            sb.DrawString(font, value, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
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

        private void DrawResourceDropdown(SpriteBatch sb, SpriteFont font, PanelLayout layout)
        {
            if (resourceDropdownCostIndex < 0 || resourceDropdownCostIndex >= newToken.ResourceCosts.Count)
                return;

            Rectangle bounds = layout.GetResourceDropdownBounds(resourceDropdownCostIndex, resourceOptions.Count);
            sb.Draw(Drawing.Pixel, bounds, Color.Black * .95f);
            sb.DrawRect(bounds, 1, Color.White * .5f);
            for (int i = 0; i < resourceOptions.Count; i++)
            {
                Rectangle option = new Rectangle(bounds.X, bounds.Y + i * EffectFieldHeight, bounds.Width, EffectFieldHeight);
                bool selected = resourceOptions[i] == newToken.ResourceCosts[resourceDropdownCostIndex].Resource;
                sb.Draw(Drawing.Pixel, option, selected ? Color.DarkSlateGray : Color.Transparent);
                sb.DrawString(font, resourceOptions[i], option.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .98f);
            }
        }

        private void DrawConstellationStarDropdown(SpriteBatch sb, SpriteFont font, PanelLayout layout)
        {
            if (!constellationStarDropdownOpen || constellationStarOptions.Count == 0)
                return;

            Rectangle bounds = layout.GetConstellationStarDropdownBounds(constellationStarOptions.Count);
            sb.Draw(Drawing.Pixel, bounds, Color.Black * .95f);
            sb.DrawRect(bounds, 1, Color.White * .5f);
            for (int i = 0; i < constellationStarOptions.Count; i++)
            {
                Rectangle option = new Rectangle(bounds.X, bounds.Y + i * EffectFieldHeight, bounds.Width, EffectFieldHeight);
                bool selected = constellationStarOptions[i] == newToken.ConstellationStar;
                sb.Draw(Drawing.Pixel, option, selected ? Color.DarkSlateGray : Color.Transparent);
                sb.DrawString(font, constellationStarOptions[i], option.Location.ToVector2() + new Vector2(7, 0), Color.White,
                    0, Vector2.Zero, 1f, SpriteEffects.None, .98f);
            }
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
    }
}
