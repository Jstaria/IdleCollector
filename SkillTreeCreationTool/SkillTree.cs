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
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTree : IScene
    {
        [JsonRequired] public int GridSpacing = 100;
        [JsonRequired] public int IconSize = 50;
        [JsonIgnore] public Point IconSizePoint;
        [JsonRequired] public string DefaultIcon = "default-icon";
        [JsonRequired] public int TokenID = 0;
        [JsonIgnore] public Vector2 iconOrigin;
        [JsonIgnore] public Texture2D iconTexture;

        [JsonProperty] private Dictionary<int, SkillTreeToken> treeTokens;
        [JsonIgnore] private Dictionary<Point, SkillTreeToken> tokenPositions;

        [JsonIgnore] public float LayerDepth { get; set; }
        [JsonIgnore] public Color Color { get; set; }


        private Point mouseStart;
        private Point cameraStart;
        private Point lastCameraPosition;

        private BasicEffectRenderable _renderable;
        public string SkillTreeScene = "SkillTreeScene";
        [JsonIgnore] private float time;
        [JsonIgnore] public float zoom = 1;
        [JsonIgnore] public float zoomTarget = 1;
        [JsonIgnore] public bool editing = false;
        

        public SkillTree()
        {
            treeTokens = new();
            
            LoadSkillTree();

            IconSizePoint = new Point(IconSize);
            iconTexture = ResourceAtlas.GetTexture(DefaultIcon);
            iconOrigin = new Vector2(iconTexture.Width / 2, iconTexture.Height / 2);

            SetFamilyTokens();

            _renderable = new BasicEffectRenderable(ResourceAtlas.GetEffect("Stars"), new Vector2(500, 300), Vector2.Zero);

            _renderable.DrawEvent += (effect) =>
            {
                _renderable.Position = -Renderer.CurrentCamera.Position.ToVector2() - Vector2.One * 10;

                effect.Parameters["iTime"].SetValue((float)time);
                effect.Parameters["iPosition"].SetValue(Renderer.CurrentCamera.Position.ToVector2() / new Vector2(480, 480));
            };

            Updater.AddToSceneExit(SkillTreeScene, () => {
                Renderer.CurrentCamera.Zoom = 1;
            });

            Renderer.AddToSceneEarlyDraw(_renderable);
        }

        private void SetFamilyTokens()
        {
            foreach (var token in treeTokens.Values)
            {
                var childIDs = token.ChildTokenIDs;
                var parentIDs = token.ParentTokenIDs;

                foreach (var child in childIDs)
                {
                    token.ChildTokens.Add(treeTokens[child]);
                }

                foreach (var parent in parentIDs)
                {
                    token.ParentTokens.Add(treeTokens[parent]);
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            //DrawDebug(sb);

            var tokens = treeTokens.Values.ToList();

            for ( int i = 0; i < treeTokens.Values.Count; i++)
            {
                SkillTreeToken token = tokens[i];

                Color drawColor = 
                    token.IsCollected ? Color.White : 
                    token.IsCollectable ? Color.White * .25f : Color.White * .05f;

                Vector2 position = GetWorldPosition(token.GridPosition).ToVector2() * zoom;
                Rectangle tokenRect = new Rectangle((position).ToPoint(), new Point((int)((float)IconSize * zoom)));
                Texture2D tex = ResourceAtlas.GetTexture(token.TokenIcon);
                Vector2 origin = new Vector2(tex.Width / 2, tex.Height / 2);
                sb.Draw(tex, tokenRect, null, drawColor, 0, origin, SpriteEffects.None, 0.5f);

                for (int j = 0; j < token.ParentTokenIDs.Count; j++)
                {
                    int parent = token.ParentTokenIDs[j];
                    Vector2 parentPos = (treeTokens[parent].GridPosition).ToVector2() * GridSpacing * zoom;

                    Vector2 point1 = Vector2.Lerp(position, parentPos, 1f / 3f);
                    Vector2 point2 = Vector2.Lerp(position, parentPos, 2f / 3f);

                    sb.DrawLineCentered(position, point1, 2 * zoom, token.IsCollected ? Color.White : drawColor, .25f);
                    sb.DrawLineCentered(point1, point2, 3 * zoom, token.IsCollected ? Color.White : drawColor, .25f);
                    sb.DrawLineCentered(point2, parentPos, 4 * zoom, token.IsCollected ? Color.White : drawColor, .25f);
                }
            }
        }

        private void DrawDebug(SpriteBatch sb)
        {
            int gridSize = 100;
            float cellSize = GridSpacing;

            float gridWidth = gridSize * cellSize;
            float gridHeight = gridSize * cellSize;

            Vector2 startPos = new Vector2(-gridWidth / 2f, -gridHeight / 2f);

            // Horizontal lines
            for (int y = 0; y <= gridSize; y++)
            {
                Vector2 pos1 = startPos + new Vector2(0, y * cellSize);
                Vector2 pos2 = startPos + new Vector2(gridWidth, y * cellSize);

                sb.DrawLineCentered(pos1, pos2, 1, Color.White * 0.05f, 0.15f);
            }

            // Vertical lines
            for (int x = 0; x <= gridSize; x++)
            {
                Vector2 pos1 = startPos + new Vector2(x * cellSize, 0);
                Vector2 pos2 = startPos + new Vector2(x * cellSize, gridHeight);

                sb.DrawLineCentered(pos1, pos2, 1, Color.White * 0.05f, 0.15f);
            }

            foreach (SkillTreeToken token in treeTokens.Values)
            {
                sb.DrawString(ResourceAtlas.GetFont("DePixelHalbfett"),token.TokenID.ToString(), token.GridPosition.ToVector2() * GridSpacing, Color.Black, 0, -Vector2.One * IconSize / 2 , .35f, SpriteEffects.None, .75f);
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {
            foreach (SkillTreeToken token in treeTokens.Values)
            {
                token.IsCollectable = token.ParentTokenIDs.All(token => treeTokens[token].IsCollected);

                if (token.ParentTokenIDs.Count == 0 || token.ParentTokenIDs == null)
                    token.IsCollectable = true;
            }
        }

        public void StandardUpdate(GameTime gameTime)
        {
            time = (float)gameTime.TotalGameTime.TotalSeconds;
            zoomTarget = Math.Clamp(zoomTarget, 0.5f, 2f);
            zoom = MathHelper.Lerp(zoom, zoomTarget, .25f);

            if (editing) return;

            if (Input.IsRightButtonDownOnce())
            {
                mouseStart = (Input.GetMouseScreenPos().ToVector2() / zoom).ToPoint();
                cameraStart = lastCameraPosition;
            }

            if (Input.IsRightButtonDown())
            {
                Point mouseCurrent = (Input.GetMouseScreenPos().ToVector2() / zoom).ToPoint();
                Point delta = mouseStart - mouseCurrent;
                Point newPos = cameraStart + delta;

                newPos.X = (int)Math.Clamp(newPos.X, -500 * zoom, 500 * zoom);
                newPos.Y = (int)Math.Clamp(newPos.Y, -500 * zoom, 500 * zoom);

                Renderer.CurrentCamera.SetTarget(newPos);
                lastCameraPosition = newPos;
            }

            if (Input.IsButtonDown(Keys.LeftControl))
            {
                if (Input.IsButtonDownOnce(Keys.S))
                    SaveSkillTree();

                if (Input.IsButtonDownOnce(Keys.Delete))
                    ResetSkillTree();
            }

            zoomTarget -= Input.GetMouseScrollDelta() * .25f;
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }

        public void CollectToken(Point gridPosition)
        {
            tokenPositions[gridPosition].Collect();
        }

        public void AddToken(Vector2 worldPosition)
        {
            Point gridPosition = GetGridPosition(worldPosition);
            AddToken(gridPosition);
        }

        public SkillTreeToken AddToken(Point gridPosition)
        {
            if (CheckForToken(gridPosition)) return null;

            if (treeTokens == null)
                treeTokens = new();

            int id = TokenID;

            treeTokens.Add(id, new SkillTreeToken(DefaultIcon, gridPosition, id));
            tokenPositions.Add(gridPosition, treeTokens[id]);
            TokenID++;

            return treeTokens[id];
        }

        public void RemoveToken(Point gridPosition)
        {
            if (!CheckForToken(gridPosition)) return;

            int id = tokenPositions[gridPosition].TokenID;
            
            foreach (SkillTreeToken token in treeTokens[id].ChildTokens)
            {
                token.RemoveParentToken(id);
            }
            foreach (SkillTreeToken token in treeTokens[id].ParentTokens)
            {
                token.RemoveChildToken(id);
            }

            treeTokens.Remove(id);
            tokenPositions.Remove(gridPosition);
        }

        public bool CheckForToken(Point gridPosition)
        {
            return tokenPositions.ContainsKey(gridPosition); 
        }

        public int GetTokenID(Point gridPosition) => tokenPositions[gridPosition].TokenID;
        public SkillTreeToken GetToken(Point gridPosition) => tokenPositions[gridPosition];

        public void SetTokenParent(SkillTreeToken parentToken, SkillTreeToken childToken)
        {
            childToken.SetParentToken(parentToken.TokenID);
            parentToken.SetChildToken(parentToken.TokenID);

            parentToken.ChildTokens.Add(childToken);
            childToken.ParentTokens.Add(parentToken);
        }

        public void SetTokenParent(Point parentPos, Point childPos)
        {
            List<SkillTreeToken> tokens = treeTokens.Values.ToList();
            SkillTreeToken parentToken = tokens.Find(x => x.GridPosition == parentPos);
            SkillTreeToken childToken = tokens.Find(x => x.GridPosition == childPos);

            SetTokenParent(parentToken, childToken);
        }

        public Point GetGridPosition()
        {
            return GetGridPosition(Input.GetMousePos().ToVector2());
        }

        public Point GetGridPosition(Vector2 worldPosition)
        {
            worldPosition /= zoom;
            int offset = GridSpacing / 2;
            worldPosition += new Vector2(worldPosition.X < 0 ? -offset : offset, worldPosition.Y < 0 ? -offset : offset);
            return (worldPosition / GridSpacing).ToPoint();
        }

        public Point GetWorldPosition(Point gridPosition)
        {
            return (gridPosition.ToVector2() * GridSpacing).ToPoint();
        }

        public void LoadSkillTree()
        {
            FileIO.ReadJsonInto(this, "Content/SaveData/SkillTree");

            tokenPositions = new();

            foreach (SkillTreeToken token in treeTokens.Values)
            {
                tokenPositions.Add(token.GridPosition, token);
            }
        }
        public void SaveSkillTree()
        {
            FileIO.WriteJsonTo(this, "Content/SaveData/SkillTree", Newtonsoft.Json.Formatting.Indented);
        }

        public void ResetSkillTree()
        {
            treeTokens = new();
            tokenPositions = new();
        }
    }
}
