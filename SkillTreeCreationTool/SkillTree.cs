using IdleCollector;
using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTree : IScene
    {
        [JsonRequired] public int GridSpacing = 100;
        [JsonRequired] public int IconSize = 50;
        [JsonRequired] public const string defaultIcon = "default-icon";
        [JsonRequired] public int TokenID = 0;

        [JsonProperty] private Dictionary<int, SkillTreeToken> treeTokens;

        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public Color Color { get; set; }


        private Point mouseStart;
        private Point cameraStart;
        private Point lastCameraPosition;

        public SkillTree() 
        {
            treeTokens = new();
            LoadSkillTree(); 
        }

        public void Draw(SpriteBatch sb)
        {
            /*{
                int gridSize = 100;
                float cellSize = GridSpacing;

                // total world size of the grid
                float gridWidth = gridSize * cellSize;
                float gridHeight = gridSize * cellSize;

                // center around (0,0)
                Vector2 startPos = new Vector2(-gridWidth / 2f, -gridHeight / 2f);

                // Horizontal lines
                for (int y = 0; y <= gridSize; y++)
                {
                    Vector2 pos1 = startPos + new Vector2(0, y * cellSize);
                    Vector2 pos2 = startPos + new Vector2(gridWidth, y * cellSize);

                    sb.DrawLine(pos1, pos2, 1, Color.White * 0.25f, 0.15f);
                }

                // Vertical lines
                for (int x = 0; x <= gridSize; x++)
                {
                    Vector2 pos1 = startPos + new Vector2(x * cellSize, 0);
                    Vector2 pos2 = startPos + new Vector2(x * cellSize, gridHeight);

                    sb.DrawLine(pos1, pos2, 1, Color.White * 0.25f, 0.15f);
                }
            }*/

            foreach (SkillTreeToken token in treeTokens.Values)
            {
                Color drawColor = token.IsCollectable ? Color.White : Color.White * .25f;

                Vector2 position = (token.GridPosition).ToVector2() * GridSpacing;
                Rectangle tokenRect = new Rectangle(position.ToPoint(), new Point(IconSize));
                Texture2D tex = ResourceAtlas.GetTexture(token.TokenIcon);
                sb.Draw(tex, tokenRect, null, drawColor, 0, new Vector2(tex.Width / 2, tex.Height / 2), SpriteEffects.None, 0.5f);

                foreach (int parent in token.ParentTokens)
                {
                    Vector2 parentPos = (treeTokens[parent].GridPosition).ToVector2() * GridSpacing;

                    sb.DrawLineCentered(position, parentPos, 2, token.IsCollected ? Color.White : drawColor, .25f);
                }
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            foreach (SkillTreeToken token in treeTokens.Values)
            {
                token.IsCollectable = token.ParentTokens.All(token => treeTokens[token].IsCollected);

                if (token.ParentTokens.Count == 0 || token.ParentTokens == null)
                    token.IsCollectable = true;
            }
        }

        public void StandardUpdate(GameTime gameTime)
        {
            if (Input.IsRightButtonDownOnce())
            {
                mouseStart = Input.GetMouseScreenPos();
                cameraStart = lastCameraPosition;
            }

            if (Input.IsRightButtonDown())
            {
                Point mouseCurrent = Input.GetMouseScreenPos();
                Point delta = mouseStart - mouseCurrent;
                Point newPos = cameraStart + delta;
                Renderer.CurrentCamera.SetTarget(newPos);
                lastCameraPosition = newPos;
            }
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }

        public void AddToken(Vector2 worldPosition)
        {
            Point gridPosition = GetGridPosition(worldPosition);
            AddToken(gridPosition);
        }

        public void AddToken(Point gridPosition)
        {
            if (treeTokens == null)
                treeTokens = new();

            int id = TokenID;

            treeTokens.Add(id, new SkillTreeToken(defaultIcon, gridPosition, id));
            TokenID++;
        }

        public void SetTokenParent(SkillTreeToken parentToken, SkillTreeToken childToken)
        {
            childToken.SetParentToken(parentToken.TokenID);
        }

        public void SetTokenParent(Point parentPos, Point childPos)
        {
            List<SkillTreeToken> tokens = treeTokens.Values.ToList();
            SkillTreeToken parentToken = tokens.Find(x => x.GridPosition == parentPos);
            SkillTreeToken childToken = tokens.Find(x => x.GridPosition == childPos);

            childToken.SetParentToken(parentToken.TokenID);
        }

        public Point GetGridPosition(Vector2 worldPosition)
        {
            return (worldPosition / GridSpacing).ToPoint();
        }

        public void LoadSkillTree()
        {
            FileIO.ReadJsonInto(this, "Content/SaveData/SkillTree");
        }
        public void SaveSkillTree()
        {
            FileIO.WriteJsonTo(this, "Content/SaveData/SkillTree", Newtonsoft.Json.Formatting.Indented);
        }
    }
}
