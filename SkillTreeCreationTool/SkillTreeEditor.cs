using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    public partial class SkillTreeEditor : IScene
    {
        private const int IconCellSize = 16;
        private const int IconsPerRow = 10;
        private const int EffectHeaderHeight = 50;
        private const int EffectHeaderSpacing = 58;
        private const int EffectFieldHeight = 46;
        private const int EffectFieldSpacing = 8;

        private static readonly (EffectField Field, string Label)[] effectFields =
        {
            (EffectField.Key, "Key"),
            (EffectField.Value, "Value"),
            (EffectField.Amount, "Amount"),
            (EffectField.Description, "Desc")
        };

        public SkillTree skillTree { get; }

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private Action<SpriteBatch> drawFunction;
        private Action<GameTime> updateFunction;
        private readonly List<SkillTreeToken> parentTokens = new();
        private SkillTreeToken newToken;
        private readonly List<IconSelect> iconSelects = new();
        private readonly List<string> resourceOptions = new();
        private int scroll;
        private readonly HashSet<int> expandedEffects = new();
        private int effectScroll;
        private int activeCostIndex = -1;
        private CostField activeCostField;
        private string activeCostAmountText = string.Empty;
        private int resourceDropdownCostIndex = -1;
        private int activeEffectIndex = -1;
        private EffectField activeEffectField;
        private string activeAmountText = string.Empty;
        private bool editedTokenWasCollected;
        private bool unlockedInEditor;
        private bool createdTokenBeingEdited;
        private IconSizeField activeIconSizeField;
        private int activeIconSlot;
        private string iconSizeText = string.Empty;
        private bool replaceIconSizeText;

        private enum EffectField
        {
            None,
            Key,
            Value,
            Amount,
            Description
        }

        private enum IconSizeField
        {
            None,
            Width,
            Height,
            Portal
        }

        private enum CostField
        {
            None,
            Resource,
            Amount
        }

        public SkillTreeEditor(SkillTree st)
        {
            skillTree = st;
            drawFunction = DrawPreview;
            updateFunction = UpdatePreview;
            LoadIcons();
            LoadResourceOptions();
        }

        public void Draw(SpriteBatch sb) => drawFunction?.Invoke(sb);
        public void DrawUi(SpriteBatch sb) { if (skillTree.editing) DrawEffectEditor(sb); }

        public void SlowUpdate(GameTime gameTime) { }
        public void StandardUpdate(GameTime gameTime) => updateFunction?.Invoke(gameTime);
        public void ControlledUpdate(GameTime gameTime) { }
    }
}
