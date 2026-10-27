using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private void LoadIcons()
        {
            foreach (string icon in ResourceAtlas.TextureCache.Keys.OrderBy(key => key))
            {
                IconSelect iconSelect = new IconSelect(icon, ResourceAtlas.TextureCache[icon], new Point(IconCellSize) * Renderer.UIScaler);
                iconSelect.button.OnClick += () =>
                {
                    IconLayer layer = GetActiveIconLayer();
                    editingTokenDescription = false;
                    layer.Icon = icon;
                    layer.Width = iconSelect.icon.Width;
                    layer.Height = iconSelect.icon.Height;
                    activeIconSizeField = IconSizeField.None;
                    activeTintLayer = activeTintChannel = -1;
                    iconSizeText = string.Empty;
                    replaceIconSizeText = false;
                };
                iconSelects.Add(iconSelect);
            }
        }

        private void LoadConstellationStarOptions()
        {
            constellationStarOptions.AddRange(ResourceAtlas.TextureCache.Keys
                .Where(key => key.StartsWith("star", StringComparison.OrdinalIgnoreCase))
                .OrderBy(key => key));

        }

        private void UpdateEdit(GameTime gameTime)
        {
            ToggleHoveredToken();
            UpdateIconSelect(gameTime);
            UpdateEffectEditor();
        }

        public void DrawEdit(SpriteBatch sb)
        {
            LayoutIconSelects();
            foreach (IconSelect iconSelect in iconSelects)
                iconSelect.Draw(sb);
        }

        private void LayoutIconSelects()
        {
            Point cellSize = new Point(IconCellSize) * Renderer.UIScaler;
            for (int i = 0; i < iconSelects.Count; i++)
                iconSelects[i].position = new Point(i % IconsPerRow * cellSize.X, i / IconsPerRow * cellSize.Y - scroll);
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

        private IconLayer GetActiveIconLayer()
        {
            newToken.IconLayers ??= new List<IconLayer>();
            if (newToken.IconLayers.Count == 0)
                newToken.IconLayers.Add(new IconLayer { Icon = skillTree.DefaultIcon, Width = skillTree.IconSize, Height = skillTree.IconSize });
            activeIconSlot = Math.Clamp(activeIconSlot, 0, newToken.IconLayers.Count - 1);
            return newToken.IconLayers[activeIconSlot];
        }

        private void DrawLayerRow(SpriteBatch sb, SpriteFont font, PanelRow row)
        {
            IconLayer layer = newToken.IconLayers[row.Index];
            headerDropdown.Draw(sb, font, row.Bounds, row.Label, row.Index == activeIconSlot, labelOffset: new Vector2(60, 2));
            Rectangle preview = GetLayerPreviewBounds(row.Bounds);
            sb.Draw(Drawing.Pixel, preview, Color.Black * .6f);
            sb.DrawRect(preview, 1, Color.White * .25f);
            if (!string.IsNullOrEmpty(layer.Icon) && ResourceAtlas.GetTexture(layer.Icon) is Texture2D texture)
                sb.Draw(texture, preview, Color.FromNonPremultiplied(layer.Tint.ToVector4()));

            DrawTintSwatch(sb, row.Bounds, layer.Tint);
            DrawLayerSizeField(sb, font, GetLayerWidthBounds(row.Bounds), "W", layer.Width, row.Index, IconSizeField.Width);
            DrawLayerSizeField(sb, font, GetLayerHeightBounds(row.Bounds), "H", layer.Height, row.Index, IconSizeField.Height);
            if (newToken.IconLayers.Count > 1)
                SkillTreeEditorHeaderDropdown.DrawRemoveButton(sb, font, SkillTreeEditorHeaderDropdown.GetRemoveButtonBounds(row.Bounds));
        }

        private void DrawLayerSizeField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, int value, int index, IconSizeField field)
        {
            bool active = activeIconSlot == index && activeIconSizeField == field;
            DrawField(sb, font, bounds, label, active ? iconSizeText : value.ToString(), active, new Vector2(bounds.X - 20, bounds.Y), Color.White);
        }

        private void HandleLayerRowClick(PanelRow row, Point mousePosition)
        {
            if (newToken.IconLayers.Count > 1 && SkillTreeEditorHeaderDropdown.GetRemoveButtonBounds(row.Bounds).Contains(mousePosition))
                RemoveLayer(row.Index);
            else if (GetLayerTintBounds(row.Bounds).Contains(mousePosition))
                ToggleLayerTintEditor(row.Index);
            else if (GetLayerWidthBounds(row.Bounds).Contains(mousePosition))
                SelectIconSizeField(row.Index, IconSizeField.Width);
            else if (GetLayerHeightBounds(row.Bounds).Contains(mousePosition))
                SelectIconSizeField(row.Index, IconSizeField.Height);
            else
                SelectLayer(row.Index);
        }

        private void RemoveLayer(int index)
        {
            newToken.IconLayers.RemoveAt(index);
            activeIconSlot = Math.Clamp(activeIconSlot, 0, newToken.IconLayers.Count - 1);
            activeIconSizeField = IconSizeField.None;
            activeTintLayer = activeTintChannel = expandedTintLayer = -1;
            iconSizeText = string.Empty;
            replaceIconSizeText = false;
        }

        private void ToggleLayerTintEditor(int index)
        {
            expandedTintLayer = expandedTintLayer == index ? -1 : index;
            activeIconSlot = index;
            activeTintLayer = activeTintChannel = -1;
            activeIconSizeField = IconSizeField.None;
            ClearCostSelection();
            ClearEffectSelection();
        }

        private void SelectLayer(int index)
        {
            activeIconSlot = index;
            resourceDropdownCostIndex = -1;
            activeIconSizeField = IconSizeField.None;
            activeTintLayer = activeTintChannel = -1;
        }

        private void UpdateIconSelect(GameTime gameTime)
        {
            LayoutIconSelects();
            Rectangle iconBounds = GetIconSelectBounds();
            Point mouse = Input.GetMouseScreenPos() * Renderer.UIScaler;
            if (Input.IsMouseInPresentationBounds && iconBounds.Contains(mouse) && iconBounds.Height >= Renderer.UIBounds.Height)
            {
                scroll = Math.Clamp(scroll + Input.GetMouseScrollDelta() * 20 * Renderer.UIScaler.Y,
                    0, Math.Max(0, iconBounds.Height - Renderer.UIBounds.Height));
                LayoutIconSelects();
            }
            if (Input.IsMouseInPresentationBounds)
                foreach (IconSelect icon in iconSelects)
                    icon.Update(gameTime);

            if (Input.IsButtonDownOnce(Keys.Escape) ||
                (Input.IsMouseInPresentationBounds && Input.IsRightButtonDownOnce()))
                CancelEditing();
            else if (Input.IsButtonDownOnce(Keys.Enter))
                EndEditing();
        }
    }
}
