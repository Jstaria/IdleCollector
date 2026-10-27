using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor
    {
        public void DrawPreview(SpriteBatch sb)
        {
            Point gridPosition = skillTree.GetGridPosition();
            Vector2 iconPosition = skillTree.GetWorldPosition(gridPosition).ToVector2() * skillTree.zoom;
            bool hasToken = skillTree.CheckForToken(gridPosition);
            Texture2D iconTexture = ResourceAtlas.GetTexture(skillTree.DefaultIcon);
            Vector2 iconSize = new Vector2(skillTree.IconSize) * skillTree.zoom;
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

            if (!Input.IsMouseInPresentationBounds)
                return;

            ToggleHoveredToken();

            if (Input.IsLeftButtonDownOnce())
            {
                if (skillTree.IsMouseOverHoverDescription(Input.GetMousePos().ToVector2()))
                    return;

                Point gridPosition = skillTree.GetGridPosition();
                if (skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken token))
                    ToggleParentToken(token);
                else
                    CreateToken(gridPosition);
            }

            if (Input.IsRightButtonDownOnce() && skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken editToken))
                BeginEditing(editToken, editToken.GridPosition);

            if (Input.IsMiddleButtonDownOnce() &&
                (Input.IsButtonDown(Keys.LeftControl) || Input.IsButtonDown(Keys.RightControl)) &&
                skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken middleToken))
                DeleteTokens(new[] { middleToken });
        }

        private void DeleteTokens(IEnumerable<SkillTreeToken> tokens)
        {
            foreach (SkillTreeToken token in tokens.ToList())
                skillTree.RemoveToken(token.GridPosition);

            parentTokens.RemoveAll(token => !skillTree.CheckForToken(token.GridPosition));
        }

        private void ToggleHoveredToken()
        {
            if (!Input.IsMouseInPresentationBounds || !Input.IsButtonDownOnce(Keys.U))
                return;

            if (skillTree.editing)
            {
                Point mouse = Input.GetMouseScreenPos() * Renderer.UIScaler;
                if (GetEffectPanelBounds().Contains(mouse) || GetIconSelectBounds().Contains(mouse) ||
                    editingTokenDescription || activeEffectField != EffectField.None || activeCostField != CostField.None ||
                    activeIconSizeField != IconSizeField.None || activeTintChannel >= 0)
                    return;
            }

            if (!skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken token))
                return;

            if (skillTree.editing && token == newToken)
            {
                bool unlock = !unlockedInEditor;
                token.IsCollected = false;
                if (unlock)
                    skillTree.UnlockToken(token.TokenID);
                unlockedInEditor = editedTokenWasCollected = unlock;
            }
            else if (token.IsCollected)
                token.IsCollected = false;
            else
                skillTree.UnlockToken(token.TokenID);
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
            editingTokenDescription = false;
            GetActiveIconLayer();
            editedTokenWasCollected = token.IsCollected;
            unlockedInEditor = token.IsCollected;
            token.IsCollected = true;
            createdTokenBeingEdited = wasCreated;
            ResetEditState();

            updateFunction = UpdateEdit;
            drawFunction = DrawEdit;
            skillTree.editing = true;

            Renderer.CurrentCamera.SetTarget(skillTree.GetWorldPosition(gridPosition));
            skillTree.zoomTarget = 1f;
        }

        private void EndEditing(bool save = true)
        {
            updateFunction = UpdatePreview;
            drawFunction = DrawPreview;
            skillTree.editing = false;
            ApplyEditedTokenState(save);
            createdTokenBeingEdited = false;
        }

        private void ResetEditState()
        {
            expandedEffects.Clear();
            effectScroll = 0;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
            activeCostIndex = -1;
            activeCostField = CostField.None;
            resourceDropdownCostIndex = -1;
            constellationStarDropdownOpen = false;
            activeConstellationTintChannel = -1;
            constellationTintText = string.Empty;
            replaceConstellationTintText = false;
            activeIconSizeField = IconSizeField.None;
            activeIconSlot = 0;
            iconSizeText = string.Empty;
            replaceIconSizeText = false;
            expandedTintLayer = activeTintLayer = activeTintChannel = -1;
        }

        private void ApplyEditedTokenState(bool save)
        {
            if (save && unlockedInEditor && !newToken.IsCollected)
                skillTree.UnlockToken(newToken.TokenID);
            else
                newToken.IsCollected = save ? unlockedInEditor : editedTokenWasCollected;
        }

        private void CancelEditing()
        {
            if (createdTokenBeingEdited)
                skillTree.RemoveToken(newToken.GridPosition);

            EndEditing(false);
        }
    }
}
