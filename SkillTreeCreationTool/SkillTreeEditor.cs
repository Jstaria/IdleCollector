using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTreeEditor : IScene
    {
        public SkillTree skillTree;

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        private delegate void DrawFunc(SpriteBatch sb);
        private DrawFunc DrawFunction;

        private delegate void UpdateFunc(GameTime sb);
        private UpdateFunc UpdateFunction;

        private List<SkillTreeToken> parentTokens;
        private SkillTreeToken newToken;
        private List<IconSelect> iconSelects;
        private int scroll = 0;

        public SkillTreeEditor(SkillTree st)
        {
            parentTokens = new();

            skillTree = st;
            DrawFunction = DrawPreview;
            UpdateFunction = UpdatePreview;

            LoadIcons();
        }

        private void LoadIcons()
        {
            iconSelects = new List<IconSelect>();

            var textures = ResourceAtlas.TextureCache;
            var textureKeys = textures.Keys.ToList().OrderBy(k => k);

            foreach (var icon in textureKeys)
            {
                IconSelect iconSelect = new IconSelect(icon, textures[icon], new Point(32));
                iconSelect.button.OnClick += () =>
                {
                    newToken.TokenIcon = icon;
                };

                iconSelects.Add(iconSelect);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            DrawFunction?.Invoke(sb);
        }

        public void DrawPreview(SpriteBatch sb)
        {
            Point gridPosition = skillTree.GetGridPosition();
            Rectangle iconRect = new Rectangle((skillTree.GetWorldPosition(gridPosition).ToVector2() * skillTree.zoom).ToPoint(), (skillTree.IconSizePoint.ToVector2() * skillTree.zoom).ToPoint());
            bool hasToken = skillTree.CheckForToken(gridPosition);
            sb.Draw(
                ResourceAtlas.GetTexture(skillTree.DefaultIcon),
                iconRect, null, hasToken ? Color.Transparent : Color.Green * .75f,
                0, skillTree.iconOrigin, SpriteEffects.None, .9f);
            if (hasToken)
                sb.DrawCircleOutline(iconRect.Location.ToVector2(), 3, 5, Color.Purple * .5f, .95f);
            foreach (var token in parentTokens)
            {
                sb.DrawCircleOutline(skillTree.GetWorldPosition(token.GridPosition).ToVector2() * skillTree.zoom, 3, 5, Color.Purple, .95f);
            }
            if (parentTokens.Count > 0)
            {
                for (int j = 0; j < parentTokens.Count; j++)
                {
                    Vector2 parentPos = skillTree.GetWorldPosition(parentTokens[j].GridPosition).ToVector2() * skillTree.zoom;
                    sb.DrawLineCentered(
                        skillTree.GetWorldPosition(
                            skillTree.GetGridPosition(Input.GetMousePos().ToVector2())).ToVector2() * skillTree.zoom,
                        parentPos, 2, Color.White * .5f, .25f);
                }
            }
        }

        private void UpdatePreview(GameTime gameTime)
        {
            if (Input.IsLeftButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();

                if (skillTree.CheckForToken(gPos))
                {
                    SkillTreeToken token = skillTree.GetToken(gPos);

                    if (parentTokens.Contains(token)) parentTokens.Remove(token);
                    else parentTokens.Add(token);
                }
                else
                {
                    newToken = skillTree.AddToken(gPos);

                    foreach (SkillTreeToken token in parentTokens)
                    {
                        skillTree.SetTokenParent(token, newToken);
                    }

                    parentTokens.Clear();

                    UpdateFunction = UpdateEdit;
                    DrawFunction = DrawEdit;

                    skillTree.editing = true;

                    Renderer.CurrentCamera.SetTarget(skillTree.GetWorldPosition((gPos.ToVector2()).ToPoint()));
                    skillTree.zoomTarget = 1f;
                    newToken.IsCollected = true;
                }

            }

            if (Input.IsMiddleButtonDownOnce())
            {
                Point gPos = skillTree.GetGridPosition();
                if (!skillTree.CheckForToken(gPos)) return;

                var token = skillTree.GetToken(gPos);

                if (parentTokens.Contains(token)) parentTokens.Remove(token);

                skillTree.RemoveToken(gPos);
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            
        }

        public void StandardUpdate(GameTime gameTime)
        {
            UpdateFunction?.Invoke(gameTime);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
        }

        private void UpdateEdit(GameTime gameTime)
        {
            UpdateIconSelect(gameTime);
        }

        public void DrawEdit(SpriteBatch sb)
        {
            DrawIconSelect(sb);
        }

        private void DrawIconSelect(SpriteBatch sb)
        {
            scroll += Input.GetMouseScrollDelta() * 20;
            scroll = (int)Math.Clamp(scroll, 0f, 100f);

            for (int i = 0; i < iconSelects.Count; i++)
            {
                IconSelect icon = iconSelects[i];

                Point camPos = Renderer.CurrentCamera.Position;

                float x = i % 5 * 32 - camPos.X;
                float y = i / 5 * 32 - camPos.Y - scroll;

                Rectangle rect = new Rectangle((int)x, (int)y, 32, 32);

                icon.button.Position = new Vector2(x, y);

                icon.Draw(sb);
            }
        }

        private void UpdateIconSelect(GameTime gt)
        {
            for (int i = 0; i < iconSelects.Count; i++)
            {
                IconSelect icon = iconSelects[i];

                icon.Update(gt);
            }

            if (Input.IsButtonDownOnce(Keys.Enter))
            {
                UpdateFunction = UpdatePreview;
                DrawFunction = DrawPreview;

                skillTree.editing = false;
                newToken.IsCollected = false;
            }
        }
    }
}
