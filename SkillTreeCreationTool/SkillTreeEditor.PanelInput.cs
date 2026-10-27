using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
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

            PanelLayout layout = GetPanelLayout();
            Point mouse = (Input.GetMouseScreenPos().ToVector2() * Renderer.UIScaler.ToVector2()).ToPoint();
            bool mouseOverPanel = Input.IsMouseInPresentationBounds && layout.Bounds.Contains(mouse);
            int previousScroll = effectScroll;
            effectScroll = Math.Clamp(effectScroll - (mouseOverPanel ? Input.GetMouseScrollDelta() * 36 : 0), 0, layout.MaximumScroll);
            if (mouseOverPanel && Input.IsLeftButtonDownOnce())
                HandleEffectEditorClick(previousScroll == effectScroll ? layout : GetPanelLayout(), mouse);
            UpdateEffectFieldText();
        }

        private void HandleEffectEditorClick(PanelLayout layout, Point mouse)
        {
            editingTokenDescription = false;
            if (TrySelectConstellationStarDropdown(layout, mouse))
                return;
            if (TrySelectResourceDropdown(layout, mouse))
                return;
            if (GetUnlockedBounds(layout.Bounds).Contains(mouse))
            {
                unlockedInEditor = !unlockedInEditor;
                return;
            }
            if (GetPortalSizeBounds(layout.Bounds).Contains(mouse))
            {
                SelectIconSizeField(-1, IconSizeField.Portal);
                return;
            }

            foreach (PanelRow row in layout.Rows)
            {
                if (!row.Bounds.Contains(mouse))
                    continue;
                switch (row.Kind)
                {
                    case PanelRowKind.Description:
                        editingTokenDescription = true;
                        ClearCostSelection();
                        ClearEffectSelection();
                        activeIconSizeField = IconSizeField.None;
                        activeTintLayer = activeTintChannel = -1;
                        break;
                    case PanelRowKind.ConstellationStar:
                        constellationStarDropdownOpen = !constellationStarDropdownOpen;
                        break;
                    case PanelRowKind.Costs:
                    case PanelRowKind.Effects:
                    case PanelRowKind.Layers:
                        if (SkillTreeEditorHeaderDropdown.GetAddButtonBounds(row.Bounds).Contains(mouse))
                            AddPanelItem(row.Kind);
                        break;
                    case PanelRowKind.Resource:
                        if (GetCostRemoveBounds(row.Bounds).Contains(mouse))
                            RemoveCost(row.Index);
                        else
                        {
                            resourceDropdownCostIndex = row.Index;
                            ClearCostSelection();
                        }
                        break;
                    case PanelRowKind.CostAmount:
                        resourceDropdownCostIndex = -1;
                        SelectCostField(row.Index, CostField.Amount);
                        break;
                    case PanelRowKind.Effect:
                        if (SkillTreeEditorHeaderDropdown.GetRemoveButtonBounds(row.Bounds).Contains(mouse))
                            RemoveEffect(row.Index);
                        else
                        {
                            if (!expandedEffects.Add(row.Index))
                                expandedEffects.Remove(row.Index);
                            ClearEffectSelection();
                            activeIconSizeField = IconSizeField.None;
                        }
                        break;
                    case PanelRowKind.EffectField:
                        SelectEffectField(row.Index, row.Field);
                        break;
                    case PanelRowKind.Layer:
                        HandleLayerRowClick(row, mouse);
                        break;
                    case PanelRowKind.LayerTint:
                        HandleTintClick(row, mouse);
                        break;
                }
                return;
            }
        }

        private void AddPanelItem(PanelRowKind kind)
        {
            switch (kind)
            {
                case PanelRowKind.Costs:
                    skillTree.AddResourceCost(newToken.TokenID, resourceOptions.FirstOrDefault() ?? string.Empty, 0);
                    resourceDropdownCostIndex = newToken.ResourceCosts.Count - 1;
                    ClearCostSelection();
                    break;
                case PanelRowKind.Effects:
                    skillTree.AddEffect(newToken.TokenID, new SkillEffectDefinition());
                    int index = newToken.Effects.Count - 1;
                    expandedEffects.Add(index);
                    SelectEffectField(index, EffectField.Key);
                    break;
                case PanelRowKind.Layers:
                    newToken.IconLayers.Add(new IconLayer());
                    activeIconSlot = newToken.IconLayers.Count - 1;
                    resourceDropdownCostIndex = -1;
                    activeIconSizeField = IconSizeField.None;
                    activeTintLayer = activeTintChannel = -1;
                    effectScroll = GetPanelLayout().MaximumScroll;
                    break;
            }
        }

        private void ClearCostSelection() { activeCostIndex = -1; activeCostField = CostField.None; }
        private void ClearEffectSelection() { activeEffectIndex = -1; activeEffectField = EffectField.None; }

        private void RemoveCost(int index)
        {
            newToken.ResourceCosts.RemoveAt(index);
            ClearCostSelection();
            resourceDropdownCostIndex = -1;
        }

        private void RemoveEffect(int index)
        {
            newToken.Effects.RemoveAt(index);
            int[] expanded = expandedEffects.Where(i => i != index).Select(i => i > index ? i - 1 : i).ToArray();
            expandedEffects.Clear();
            expandedEffects.UnionWith(expanded);
            ClearEffectSelection();
        }

        private bool TrySelectResourceDropdown(PanelLayout layout, Point mouse)
        {
            if (resourceDropdownCostIndex < 0)
                return false;

            Rectangle bounds = layout.GetResourceDropdownBounds(resourceDropdownCostIndex, resourceOptions.Count);
            bool selected = bounds.Contains(mouse);
            int index = (mouse.Y - bounds.Y) / EffectFieldHeight;
            if (selected && index >= 0 && index < resourceOptions.Count)
                newToken.ResourceCosts[resourceDropdownCostIndex].Resource = resourceOptions[index];
            resourceDropdownCostIndex = -1;
            return selected;
        }

        private bool TrySelectConstellationStarDropdown(PanelLayout layout, Point mouse)
        {
            if (!constellationStarDropdownOpen)
                return false;

            Rectangle bounds = layout.GetConstellationStarDropdownBounds(constellationStarOptions.Count);
            if (!bounds.Contains(mouse))
            {
                constellationStarDropdownOpen = false;
                return false;
            }

            int index = (mouse.Y - bounds.Y) / EffectFieldHeight;
            if (index >= 0 && index < constellationStarOptions.Count)
                newToken.ConstellationStar = constellationStarOptions[index];
            constellationStarDropdownOpen = false;
            return true;
        }

        private void SelectEffectField(int index, EffectField field)
        {
            activeTintLayer = activeTintChannel = -1;
            activeEffectIndex = index;
            activeEffectField = field;
            ClearCostSelection();
            activeIconSizeField = IconSizeField.None;
            activeAmountText = field == EffectField.Amount ? newToken.Effects[index].Amount.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private void SelectCostField(int index, CostField field)
        {
            activeTintLayer = activeTintChannel = -1;
            activeCostIndex = index;
            activeCostField = field;
            ClearEffectSelection();
            activeIconSizeField = IconSizeField.None;
            activeCostAmountText = field == CostField.Amount ? newToken.ResourceCosts[index].Amount.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private void UpdateEffectFieldText()
        {
            string input = Game1.ConsumeTextInput();
            bool backspace = Input.IsButtonDownOnce(Keys.Back);
            if (editingTokenDescription)
                newToken.Description = SkillTreeEditorTextInput.Edit(newToken.Description ?? string.Empty, input, backspace, 2000);
            else if (activeTintLayer >= 0 && activeTintLayer < newToken.IconLayers.Count && activeTintChannel >= 0)
                UpdateTint(input, backspace);
            else if (activeIconSizeField != IconSizeField.None)
                UpdateIconSize(input, backspace);
            else if (activeCostIndex >= 0 && activeCostIndex < newToken.ResourceCosts.Count)
                UpdateCostFieldText(input, backspace);
            else if (activeEffectIndex >= 0 && activeEffectIndex < newToken.Effects.Count)
            {
                SkillEffectDefinition effect = newToken.Effects[activeEffectIndex];
                string value = activeEffectField == EffectField.Amount ? activeAmountText : GetEffectDisplayValue(effect, activeEffectField);
                int limit = activeEffectField == EffectField.Description ? 240 : 80;
                SetEffectFieldValue(effect, activeEffectField, SkillTreeEditorTextInput.Edit(value, input, backspace, limit));
            }
        }

        private void UpdateCostFieldText(string input, bool backspace)
        {
            ResourceCost cost = newToken.ResourceCosts[activeCostIndex];
            bool numeric = activeCostField == CostField.Amount;
            string value = SkillTreeEditorTextInput.Edit(numeric ? activeCostAmountText : cost.Resource, input, backspace, 40,
                numeric ? char.IsDigit : character => !char.IsControl(character));
            if (!numeric)
                cost.Resource = value;
            else
            {
                activeCostAmountText = value;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                    cost.Amount = Math.Max(0, amount);
            }
        }

        private void UpdateIconSize(string input, bool backspace)
        {
            iconSizeText = SkillTreeEditorTextInput.Edit(iconSizeText, input, backspace, 4, ref replaceIconSizeText, char.IsDigit);
            if (!int.TryParse(iconSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                return;
            size = Math.Max(1, size);
            switch (activeIconSizeField)
            {
                case IconSizeField.Width: GetActiveIconLayer().Width = size; break;
                case IconSizeField.Height: GetActiveIconLayer().Height = size; break;
                case IconSizeField.Portal: newToken.PortalSize = size; break;
            }
        }

        private void SelectIconSizeField(int index, IconSizeField field)
        {
            activeTintLayer = activeTintChannel = -1;
            activeIconSizeField = field;
            if (field != IconSizeField.Portal)
                activeIconSlot = index;
            int value = field == IconSizeField.Portal ? newToken.PortalSize :
                field == IconSizeField.Width ? GetActiveIconLayer().Width : GetActiveIconLayer().Height;
            iconSizeText = value.ToString(CultureInfo.InvariantCulture);
            replaceIconSizeText = true;
            ClearEffectSelection();
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
                case EffectField.Key: effect.Key = value; break;
                case EffectField.Value: effect.Value = value; break;
                case EffectField.Description: effect.SkillEffectDescription = value; break;
                case EffectField.Amount:
                    activeAmountText = value;
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                        effect.Amount = amount;
                    break;
            }
        }
    }
}
