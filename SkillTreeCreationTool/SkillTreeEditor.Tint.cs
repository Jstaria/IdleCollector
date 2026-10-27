using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Globalization;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private static readonly string[] tintChannelLabels = { "R", "G", "B", "A" };

        private static byte GetTintChannel(Color tint, int channel) => channel switch
        {
            0 => tint.R, 1 => tint.G, 2 => tint.B, _ => tint.A
        };

        private void DrawTintSwatch(SpriteBatch sb, Rectangle row, Color tint)
        {
            Rectangle bounds = GetLayerTintBounds(row);
            sb.Draw(Drawing.Pixel, bounds, Color.DarkGray);
            sb.Draw(Drawing.Pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width / 2, bounds.Height / 2), Color.Gray);
            sb.Draw(Drawing.Pixel, new Rectangle(bounds.Center.X, bounds.Center.Y, bounds.Width / 2, bounds.Height / 2), Color.Gray);
            sb.Draw(Drawing.Pixel, bounds, Color.FromNonPremultiplied(tint.ToVector4()));
            sb.DrawRect(bounds, 1, Color.White * .5f);
        }

        private void DrawLayerTint(SpriteBatch sb, SpriteFont font, PanelRow row)
        {
            Color tint = newToken.IconLayers[row.Index].Tint;
            for (int channel = 0; channel < tintChannelLabels.Length; channel++)
            {
                Rectangle bounds = GetTintChannelBounds(row.Bounds, channel);
                bool active = activeTintLayer == row.Index && activeTintChannel == channel;
                string value = active ? tintText : GetTintChannel(tint, channel).ToString(CultureInfo.InvariantCulture);
                DrawField(sb, font, bounds, tintChannelLabels[channel], value, active,
                    new Vector2(bounds.X - 24, bounds.Y), Color.LightGray);
            }
        }

        private void DrawConstellationTint(SpriteBatch sb, SpriteFont font, PanelRow row)
        {
            Color tint = newToken.ConstellationColor;
            for (int channel = 0; channel < tintChannelLabels.Length; channel++)
            {
                Rectangle bounds = GetTintChannelBounds(row.Bounds, channel);
                bool active = activeConstellationTintChannel == channel;
                string value = active ? constellationTintText : GetTintChannel(tint, channel).ToString(CultureInfo.InvariantCulture);
                DrawField(sb, font, bounds, tintChannelLabels[channel], value, active,
                    new Vector2(bounds.X - 24, bounds.Y), Color.LightGray);
            }
        }

        private void HandleTintClick(PanelRow row, Point mouse)
        {
            for (int channel = 0; channel < tintChannelLabels.Length; channel++)
            {
                if (!GetTintChannelBounds(row.Bounds, channel).Contains(mouse))
                    continue;
                activeTintLayer = activeIconSlot = row.Index;
                activeTintChannel = channel;
                tintText = GetTintChannel(newToken.IconLayers[row.Index].Tint, channel).ToString(CultureInfo.InvariantCulture);
                replaceTintText = true;
                ClearCostSelection();
                ClearEffectSelection();
                activeIconSizeField = IconSizeField.None;
                return;
            }
        }

        private void HandleConstellationTintClick(PanelRow row, Point mouse)
        {
            for (int channel = 0; channel < tintChannelLabels.Length; channel++)
            {
                if (!GetTintChannelBounds(row.Bounds, channel).Contains(mouse))
                    continue;

                activeConstellationTintChannel = channel;
                constellationTintText = GetTintChannel(newToken.ConstellationColor, channel).ToString(CultureInfo.InvariantCulture);
                replaceConstellationTintText = true;
                ClearCostSelection();
                ClearEffectSelection();
                activeIconSizeField = IconSizeField.None;
                return;
            }
        }

        private void UpdateTint(string input, bool backspace)
        {
            tintText = SkillTreeEditorTextInput.Edit(tintText, input, backspace, 3, ref replaceTintText, char.IsDigit);
            if (!int.TryParse(tintText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                return;
            byte value = (byte)Math.Clamp(number, 0, 255);
            IconLayer layer = newToken.IconLayers[activeTintLayer];
            Color tint = layer.Tint;
            switch (activeTintChannel)
            {
                case 0: tint.R = value; break;
                case 1: tint.G = value; break;
                case 2: tint.B = value; break;
                case 3: tint.A = value; break;
            }
            layer.Tint = tint;
            if (newToken.ParentTokenIDs.Count == 0 && activeTintLayer == 0)
                newToken.ConstellationColor = tint;
        }

        private void UpdateConstellationTint(string input, bool backspace)
        {
            constellationTintText = SkillTreeEditorTextInput.Edit(constellationTintText, input, backspace, 3,
                ref replaceConstellationTintText, char.IsDigit);
            if (!int.TryParse(constellationTintText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                return;

            byte value = (byte)Math.Clamp(number, 0, 255);
            Color tint = newToken.ConstellationColor;
            switch (activeConstellationTintChannel)
            {
                case 0: tint.R = value; break;
                case 1: tint.G = value; break;
                case 2: tint.B = value; break;
                case 3: tint.A = value; break;
            }
            newToken.ConstellationColor = tint;
            if (newToken.IconLayers.Count > 0)
                newToken.IconLayers[0].Tint = tint;
        }
    }
}
