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

            if (Input.IsLeftButtonDownOnce())
            {
                Point gridPosition = skillTree.GetGridPosition();
                if (skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken token))
                    ToggleParentToken(token);
                else
                    CreateToken(gridPosition);
            }

            if (Input.IsRightButtonDownOnce() && skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken editToken))
                BeginEditing(editToken, editToken.GridPosition);

            if (Input.IsMiddleButtonDownOnce() && skillTree.TryGetTokenAt(Input.GetMousePos().ToVector2(), out SkillTreeToken middleToken))
            {
                if (Input.IsButtonDown(Keys.LeftControl) || Input.IsButtonDown(Keys.RightControl))
                    skillTree.UnlockToken(middleToken.TokenID);
                else
                    DeleteTokens(new[] { middleToken });
            }
        }

        private void DeleteTokens(IEnumerable<SkillTreeToken> tokens)
        {
            foreach (SkillTreeToken token in tokens.ToList())
                skillTree.RemoveToken(token.GridPosition);

            parentTokens.RemoveAll(token => !skillTree.CheckForToken(token.GridPosition));
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
            GetActiveIconLayer();
            editedTokenWasCollected = token.IsCollected;
            unlockedInEditor = token.IsCollected;
            token.IsCollected = true;
            createdTokenBeingEdited = wasCreated;
            expandedEffects.Clear();
            effectScroll = 0;
            activeEffectIndex = -1;
            activeEffectField = EffectField.None;
            activeCostIndex = -1;
            activeCostField = CostField.None;
            resourceDropdownCostIndex = -1;
            activeIconSizeField = IconSizeField.None;
            activeIconSlot = 0;
            iconSizeText = string.Empty;
            replaceIconSizeText = false;

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
            if (save && unlockedInEditor && !newToken.IsCollected)
                skillTree.UnlockToken(newToken.TokenID);
            else
                newToken.IsCollected = save ? unlockedInEditor : editedTokenWasCollected;
            createdTokenBeingEdited = false;
        }

        private void CancelEditing()
        {
            if (createdTokenBeingEdited)
                skillTree.RemoveToken(newToken.GridPosition);

            EndEditing(false);
        }
    }
}
