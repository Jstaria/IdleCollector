using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private enum PanelRowKind { Description, ConstellationStar, ConstellationTint, Costs, Resource, CostAmount, Effects, Effect, EffectField, Layers, Layer, LayerTint }

        private readonly record struct PanelRow(PanelRowKind Kind, Rectangle Bounds, int Index = -1,
            EffectField Field = EffectField.None, string Label = "", string Value = "");

        private sealed record PanelLayout(Rectangle Bounds, List<PanelRow> Rows, int MaximumScroll)
        {
            public Rectangle GetResourceDropdownBounds(int costIndex, int optionCount)
            {
                Rectangle resource = Rows.Find(row => row.Kind == PanelRowKind.Resource && row.Index == costIndex).Bounds;
                return new Rectangle(resource.X, resource.Bottom, resource.Width, optionCount * EffectFieldHeight);
            }

            public Rectangle GetConstellationStarDropdownBounds(int optionCount)
            {
                Rectangle star = Rows.Find(row => row.Kind == PanelRowKind.ConstellationStar).Bounds;
                return new Rectangle(star.X, star.Bottom, star.Width, optionCount * EffectFieldHeight);
            }
        }

        private PanelLayout GetPanelLayout()
        {
            Rectangle panel = GetEffectPanelBounds();
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");
            List<PanelRow> rows = new();
            int startY = panel.Y + EffectHeaderSpacing - effectScroll;
            int y = startY;

            void Header(PanelRowKind kind, string label, int index = -1)
            {
                rows.Add(new PanelRow(kind, SkillTreeEditorHeaderDropdown.GetBounds(panel, y), index, Label: label));
                y += EffectHeaderSpacing;
            }

            void Field(PanelRowKind kind, int index, string label, string value, EffectField field = EffectField.None)
            {
                int height = GetEffectFieldHeight(font, panel, field, value);
                if (field == EffectField.Description)
                    value = WrapText(font, value, panel.Width - 191);
                rows.Add(new PanelRow(kind, new Rectangle(panel.X + 165, y, panel.Width - 177, height), index, field, label, value));
                y += height + EffectFieldSpacing;
            }

            string description = WrapText(font, newToken.Description ?? string.Empty, panel.Width - 34);
            int descriptionLines = description.Count(character => character == '\n') + 1;
            rows.Add(new PanelRow(PanelRowKind.Description,
                new Rectangle(panel.X + 10, y + font.LineSpacing, panel.Width - 20,
                    Math.Max(112, descriptionLines * font.LineSpacing + 16)), Label: "Desc", Value: description));
            y = rows[0].Bounds.Bottom + EffectFieldSpacing;

            Header(PanelRowKind.ConstellationStar, $"Constellation Star: {newToken.ConstellationStar}");
            if (newToken.ParentTokenIDs.Count == 0)
            {
                rows.Add(new PanelRow(PanelRowKind.ConstellationTint,
                    new Rectangle(panel.X + 10, y, panel.Width - 20, EffectFieldHeight)));
                y += EffectFieldHeight + EffectFieldSpacing;
            }
            Header(PanelRowKind.Costs, "Costs");
            for (int i = 0; i < newToken.ResourceCosts.Count; i++)
            {
                ResourceCost cost = newToken.ResourceCosts[i];
                Field(PanelRowKind.Resource, i, "Resource", cost.Resource);
                Field(PanelRowKind.CostAmount, i, "Amount", cost.Amount.ToString(CultureInfo.InvariantCulture));
                y += 4;
            }

            Header(PanelRowKind.Effects, "Effects");
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                Header(PanelRowKind.Effect, $"Effect {i + 1}", i);
                if (!expandedEffects.Contains(i))
                    continue;
                foreach ((EffectField field, string label) in effectFields)
                    Field(PanelRowKind.EffectField, i, label, GetEffectDisplayValue(newToken.Effects[i], field), field);
                y += 4;
            }

            Header(PanelRowKind.Layers, "Layers");
            for (int i = newToken.IconLayers.Count - 1; i >= 0; i--)
            {
                Header(PanelRowKind.Layer, $"Layer {i + 1}", i);
                if (expandedTintLayer == i)
                {
                    rows.Add(new PanelRow(PanelRowKind.LayerTint,
                        new Rectangle(panel.X + 10, y, panel.Width - 20, EffectFieldHeight), i));
                    y += EffectFieldHeight + EffectFieldSpacing;
                }
            }

            return new PanelLayout(panel, rows, Math.Max(0, y - startY - (panel.Height - 66)));
        }

        private static Rectangle GetEffectPanelBounds() => new Rectangle(Renderer.UIBounds.Width - 728, 8, 720, Renderer.UIBounds.Height - 16);
        private static Rectangle GetPortalSizeBounds(Rectangle panel) => new Rectangle(panel.X + 340, panel.Y + 8, 98, 42);
        private static Rectangle GetUnlockedBounds(Rectangle panel) => new Rectangle(panel.X + 625, panel.Y + 8, 42, 42);
        private static Rectangle GetCostRemoveBounds(Rectangle field) => new Rectangle(field.Right - 38, field.Y, 38, 38);
        private static Rectangle GetLayerWidthBounds(Rectangle row) => new Rectangle(row.Right - 270, row.Y + 4, 72, 42);
        private static Rectangle GetLayerHeightBounds(Rectangle row) => new Rectangle(row.Right - 152, row.Y + 4, 72, 42);
        private static Rectangle GetLayerPreviewBounds(Rectangle row) => new Rectangle(row.X + 1, row.Y + 1, 48, 48);
        private static Rectangle GetLayerTintBounds(Rectangle row) => new Rectangle(row.Right - 342, row.Y + 6, 38, 38);
        private static Rectangle GetTintChannelBounds(Rectangle row, int channel) => new Rectangle(row.X + 132 + channel * 140, row.Y, 78, EffectFieldHeight);
    }
}
