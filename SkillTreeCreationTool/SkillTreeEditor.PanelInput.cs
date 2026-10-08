using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Globalization;
using System.Linq;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        private void UpdateEffectEditor()
        {
            if (newToken == null)
                return;

            Rectangle panel = GetEffectPanelBounds();
            
            Point mousePosition = (Input.GetMouseScreenPos().ToVector2() * Renderer.UIScaler.ToVector2()).ToPoint();
            int maximumScroll = GetMaximumEffectScroll(panel);
            bool mouseInPresentation = Input.IsMouseInPresentationBounds;
            if (mouseInPresentation && panel.Contains(mousePosition))
                effectScroll = Math.Clamp(effectScroll - Input.GetMouseScrollDelta() * 36, 0, maximumScroll);
            else
                effectScroll = Math.Clamp(effectScroll, 0, maximumScroll);

            if (mouseInPresentation && panel.Contains(mousePosition) && Input.IsLeftButtonDownOnce())
                HandleEffectEditorClick(panel, mousePosition);

            UpdateEffectFieldText();
        }

        private int GetMaximumEffectScroll(Rectangle panel)
        {
            SpriteFont font = ResourceAtlas.GetFont("EffectEditor");
            int contentHeight = EffectHeaderSpacing;
            contentHeight += newToken.ResourceCosts.Count * ((EffectFieldHeight + EffectFieldSpacing) * 2 + 4);
            contentHeight += EffectHeaderSpacing;
            for (int i = 0; i < newToken.Effects.Count; i++)
            {
                contentHeight += EffectHeaderSpacing;
                if (!expandedEffects.Contains(i))
                    continue;

                foreach ((EffectField field, _) in effectFields)
                    contentHeight += GetEffectFieldHeight(font, panel, field, GetEffectDisplayValue(newToken.Effects[i], field)) + EffectFieldSpacing;
                contentHeight += 4;
            }

            contentHeight += EffectHeaderSpacing * (1 + newToken.IconLayers.Count);
            return Math.Max(0, contentHeight - (panel.Height - 66));
        }

        private void HandleEffectEditorClick(Rectangle panel, Point mousePosition)
        {
            if (TrySelectResourceDropdown(panel, mousePosition))
                return;

            if (GetUnlockedBounds(panel).Contains(mousePosition))
            {
                unlockedInEditor = !unlockedInEditor;
                return;
            }

            if (GetPortalSizeBounds(panel).Contains(mousePosition))
            {
                SelectIconSizeField(-1, IconSizeField.Portal);
                return;
            }

            Rectangle addCostButton = new Rectangle(panel.Right - 48, panel.Y + 66 - effectScroll, 38, 38);
            if (addCostButton.Contains(mousePosition))
            {
                skillTree.AddResourceCost(newToken.TokenID, resourceOptions.FirstOrDefault() ?? string.Empty, 0);
                resourceDropdownCostIndex = newToken.ResourceCosts.Count - 1;
                activeCostIndex = -1;
                activeCostField = CostField.None;
                return;
            }

            int y = panel.Y + EffectHeaderSpacing - effectScroll + EffectHeaderSpacing;
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

            HandleIconLayerClick(panel, ref y, mousePosition);
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
            {
                if (!char.IsDigit(character))
                    continue;
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
            else if (activeIconSizeField == IconSizeField.Height)
                SetIconHeightForSlot(activeIconSlot, Math.Max(1, size));
            else
                newToken.PortalSize = Math.Max(1, size);
        }

        private void SelectIconSizeField(int layerIndex, IconSizeField field)
        {
            activeIconSizeField = field;
            if (field != IconSizeField.Portal)
                activeIconSlot = layerIndex;

            int value = field == IconSizeField.Portal ? newToken.PortalSize :
                field == IconSizeField.Width ? GetActiveIconLayer().Width : GetActiveIconLayer().Height;
            iconSizeText = value.ToString(CultureInfo.InvariantCulture);
            replaceIconSizeText = true;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
        }

        private static string GetEffectDisplayValue(SkillEffectDefinition effect, EffectField field) => field switch
        {
            EffectField.Key => effect.Key,
            EffectField.Value => effect.Value,
            EffectField.Amount => effect.Amount.ToString(CultureInfo.InvariantCulture),
            EffectField.Description => effect.SkillEffectDescription,
            _ => string.Empty
        };

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
    }
}
