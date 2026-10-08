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
        private readonly SkillTreeEditorHeaderDropdown layersHeaderDropdown = new("Layers");
        private readonly List<SkillTreeEditorHeaderDropdown> layerOptionHeaderDropdowns = new();

        private SkillTreeEditorHeaderDropdown GetLayerOptionHeaderDropdown(int index)
        {
            while (layerOptionHeaderDropdowns.Count <= index)
                layerOptionHeaderDropdowns.Add(new SkillTreeEditorHeaderDropdown(string.Empty));

            SkillTreeEditorHeaderDropdown header = layerOptionHeaderDropdowns[index];
            header.Label = $"Layer {index + 1}";
            header.LabelOffset = new Vector2(60, 2);
            return header;
        }

        private void LoadIcons()
        {
            foreach (string icon in ResourceAtlas.TextureCache.Keys.OrderBy(key => key))
            {
                IconSelect iconSelect = new IconSelect(icon, ResourceAtlas.TextureCache[icon], new Point(IconCellSize));
                iconSelect.button.OnClick += () =>
                {
                    SetIconForActiveSlot(icon);
                    SetIconSizeForSlot(activeIconSlot, iconSelect.icon.Width, iconSelect.icon.Height);
                    activeIconSizeField = IconSizeField.None;
                    iconSizeText = string.Empty;
                    replaceIconSizeText = false;
                };
                iconSelects.Add(iconSelect);
            }
        }

        private void UpdateEdit(GameTime gameTime)
        {
            UpdateIconSelect(gameTime);
            UpdateEffectEditor();
        }

        public void DrawEdit(SpriteBatch sb) => DrawIconSelect(sb);

        private void DrawIconSelect(SpriteBatch sb)
        {
            LayoutIconSelects();
            foreach (IconSelect iconSelect in iconSelects)
                iconSelect.Draw(sb);
        }

        private void LayoutIconSelects()
        {
            Point cameraPosition = Renderer.CurrentCamera.Position;
            for (int i = 0; i < iconSelects.Count; i++)
            {
                int x = i % IconsPerRow * IconCellSize - cameraPosition.X;
                int y = i / IconsPerRow * IconCellSize - cameraPosition.Y - scroll;
                iconSelects[i].position = new Point(x, y);
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

        private IconLayer GetActiveIconLayer() => GetIconLayer(activeIconSlot);

        private IconLayer GetIconLayer(int slot)
        {
            newToken.IconLayers ??= new List<IconLayer>();
            if (newToken.IconLayers.Count == 0)
            {
                newToken.IconLayers.Add(new IconLayer
                {
                    Icon = skillTree.DefaultIcon,
                    Width = skillTree.IconSize,
                    Height = skillTree.IconSize
                });
            }

            activeIconSlot = Math.Clamp(slot, 0, newToken.IconLayers.Count - 1);
            return newToken.IconLayers[activeIconSlot];
        }

        private void SetIconSizeForSlot(int slot, int width, int height)
        {
            IconLayer layer = GetIconLayer(slot);
            layer.Width = width;
            layer.Height = height;
        }

        private void SetIconWidthForSlot(int slot, int width) => GetIconLayer(slot).Width = width;
        private void SetIconHeightForSlot(int slot, int height) => GetIconLayer(slot).Height = height;
        private void SetIconForActiveSlot(string icon) => GetActiveIconLayer().Icon = icon;

        private static Rectangle GetLayersHeaderBounds(Rectangle panel, int y) =>
            new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);

        private static Rectangle GetLayerAddBounds(Rectangle panel, int y)
        {
            Rectangle header = GetLayersHeaderBounds(panel, y);
            return new Rectangle(header.Right - 48, header.Y + 6, 38, 38);
        }

        private static Rectangle GetLayerItemBounds(Rectangle panel, int y) =>
            new Rectangle(panel.X + 10, y, panel.Width - 20, EffectHeaderHeight);

        private static Rectangle GetLayerWidthBounds(Rectangle layerBounds) =>
            new Rectangle(layerBounds.Right - 270, layerBounds.Y + 4, 72, 42);

        private static Rectangle GetLayerHeightBounds(Rectangle layerBounds) =>
            new Rectangle(layerBounds.Right - 152, layerBounds.Y + 4, 72, 42);

        private static Rectangle GetLayerRemoveBounds(Rectangle layerBounds) =>
            new Rectangle(layerBounds.Right - 42, layerBounds.Y + 6, 34, 38);

        private static Rectangle GetLayerPreviewBounds(Rectangle layerBounds) =>
            new Rectangle(layerBounds.X + 1, layerBounds.Y + 1, 48, 48);

        private static void DrawLayerPreview(SpriteBatch sb, Rectangle bounds, string icon)
        {
            sb.Draw(Drawing.Pixel, bounds, Color.Black * .6f);
            sb.DrawRect(bounds, 1, Color.White * .25f);
            if (string.IsNullOrEmpty(icon))
                return;

            Texture2D texture = ResourceAtlas.GetTexture(icon);
            if (texture != null)
                sb.Draw(texture, bounds, Color.White);
        }

        private void DrawLayerSizeField(SpriteBatch sb, SpriteFont font, Rectangle bounds, string label, int value, int layerIndex, IconSizeField field)
        {
            bool active = activeIconSlot == layerIndex && activeIconSizeField == field;
            sb.DrawString(font, label, new Vector2(bounds.X - 20, bounds.Y), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);
            sb.Draw(Drawing.Pixel, bounds, active ? Color.DarkBlue : Color.Black * .6f);
            sb.DrawRect(bounds, 1, active ? Color.White : Color.White * .25f);
            string displayedValue = active ? iconSizeText : value.ToString();
            sb.DrawString(font, displayedValue, bounds.Location.ToVector2() + new Vector2(7, 0), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
        }

        private void DrawIconLayerSection(SpriteBatch sb, SpriteFont font, Rectangle panel, ref int y)
        {
            Rectangle header = GetLayersHeaderBounds(panel, y);
            layersHeaderDropdown.IsExpanded = true;
            layersHeaderDropdown.Layout(header, false, true);
            layersHeaderDropdown.Draw(sb, font);
            y += EffectHeaderSpacing;

            for (int i = newToken.IconLayers.Count - 1; i >= 0; i--)
            {
                IconLayer layer = newToken.IconLayers[i];
                Rectangle layerBounds = GetLayerItemBounds(panel, y);
                SkillTreeEditorHeaderDropdown layerHeader = GetLayerOptionHeaderDropdown(i);
                layerHeader.IsExpanded = i == activeIconSlot;
                layerHeader.Layout(layerBounds, false, false);
                layerHeader.Draw(sb, font);
                DrawLayerPreview(sb, GetLayerPreviewBounds(layerBounds), layer.Icon);
                DrawLayerSizeField(sb, font, GetLayerWidthBounds(layerBounds), "W", layer.Width, i, IconSizeField.Width);
                DrawLayerSizeField(sb, font, GetLayerHeightBounds(layerBounds), "H", layer.Height, i, IconSizeField.Height);
                if (newToken.IconLayers.Count > 1)
                    DrawRemoveButton(sb, font, GetLayerRemoveBounds(layerBounds));
                y += EffectHeaderSpacing;
            }
        }

        private bool HandleIconLayerClick(Rectangle panel, ref int y, Point mousePosition)
        {
            if (GetLayerAddBounds(panel, y).Contains(mousePosition))
            {
                newToken.IconLayers.Add(new IconLayer());
                activeIconSlot = newToken.IconLayers.Count - 1;
                resourceDropdownCostIndex = -1;
                activeIconSizeField = IconSizeField.None;
                effectScroll = GetMaximumEffectScroll(panel);
                return true;
            }

            y += EffectHeaderSpacing;
            for (int i = newToken.IconLayers.Count - 1; i >= 0; i--)
            {
                Rectangle layerBounds = GetLayerItemBounds(panel, y);
                if (newToken.IconLayers.Count > 1 && GetLayerRemoveBounds(layerBounds).Contains(mousePosition))
                {
                    RemoveIconLayer(i);
                    return true;
                }

                if (GetLayerWidthBounds(layerBounds).Contains(mousePosition))
                {
                    SelectIconSizeField(i, IconSizeField.Width);
                    return true;
                }

                if (GetLayerHeightBounds(layerBounds).Contains(mousePosition))
                {
                    SelectIconSizeField(i, IconSizeField.Height);
                    return true;
                }

                if (layerBounds.Contains(mousePosition))
                {
                    activeIconSlot = i;
                    resourceDropdownCostIndex = -1;
                    activeIconSizeField = IconSizeField.None;
                    return true;
                }
                y += EffectHeaderSpacing;
            }

            return false;
        }

        private void RemoveIconLayer(int index)
        {
            if (newToken.IconLayers.Count <= 1)
                return;

            newToken.IconLayers.RemoveAt(index);
            activeIconSlot = Math.Clamp(activeIconSlot, 0, newToken.IconLayers.Count - 1);
            activeIconSizeField = IconSizeField.None;
            iconSizeText = string.Empty;
            replaceIconSizeText = false;
        }

        private void UpdateIconSelect(GameTime gameTime)
        {
            LayoutIconSelects();
            Rectangle iconBounds = GetIconSelectBounds();
            if (Input.IsMouseInPresentationBounds && iconBounds.Contains(Input.GetMousePos()) && iconBounds.Height >= Renderer.RenderSize.Y)
            {
                scroll = Math.Clamp(scroll + Input.GetMouseScrollDelta() * 20, 0, 100);
                LayoutIconSelects();
            }

            if (Input.IsMouseInPresentationBounds)
                foreach (IconSelect icon in iconSelects)
                    icon.Update(gameTime);

            if (Input.IsButtonDownOnce(Keys.Escape))
                CancelEditing();
            else if (Input.IsButtonDownOnce(Keys.Enter))
                EndEditing();
        }
    }
}
