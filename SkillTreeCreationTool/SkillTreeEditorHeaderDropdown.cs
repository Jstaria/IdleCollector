using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkillTreeCreationTool
{
    public sealed class SkillTreeEditorHeaderDropdown
    {
        public string Label { get; set; }
        public Rectangle Bounds { get; private set; }
        public Vector2 LabelOffset { get; set; } = new Vector2(10, 2);
        public bool IsExpanded { get; set; }
        public bool ShowsExpansionIndicator { get; private set; }
        public bool ShowsAddButton { get; private set; }

        public Rectangle AddButtonBounds => new Rectangle(Bounds.Right - 48, Bounds.Y + 6, 38, 38);

        public SkillTreeEditorHeaderDropdown(string label)
        {
            Label = label;
        }

        public void Layout(Rectangle bounds, bool showsExpansionIndicator, bool showsAddButton)
        {
            Bounds = bounds;
            ShowsExpansionIndicator = showsExpansionIndicator;
            ShowsAddButton = showsAddButton;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            spriteBatch.Draw(Drawing.Pixel, Bounds, IsExpanded ? Color.DarkSlateGray : Color.Black * .6f);
            string prefix = ShowsExpansionIndicator ? (IsExpanded ? "- " : "+ ") : string.Empty;
            spriteBatch.DrawString(font, prefix + Label, Bounds.Location.ToVector2() + LabelOffset, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .95f);

            if (!ShowsAddButton)
                return;

            spriteBatch.Draw(Drawing.Pixel, AddButtonBounds, Color.Green * .75f);
            spriteBatch.DrawString(font, "+", AddButtonBounds.Location.ToVector2() + new Vector2(11, -6), Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, .96f);
        }
    }
}
