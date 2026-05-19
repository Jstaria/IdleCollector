using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillTreeCreationTool
{
    public class SkillTreeToken
    {
        public string TokenIcon { get; set; }
        public int TokenID { get; set; }
        public Point GridPosition { get; set; }
        public List<int> ParentTokenIDs { get; set; }
        public List<int> ChildTokenIDs { get; set; }
        [JsonIgnore] public List<SkillTreeToken> ChildTokens { get; set; }
        [JsonIgnore] public List<SkillTreeToken> ParentTokens { get; set; }

        public bool IsCollected { get; set; }
        public bool IsCollectable {  get; set; }

        public SkillTreeToken(string iconName, Point gridPosition, int tokenID)
        {
            TokenIcon = iconName;
            TokenID = tokenID;
            GridPosition = gridPosition;
            ParentTokenIDs = new List<int>();
            ChildTokenIDs = new List<int>();
            ChildTokens = new List<SkillTreeToken>();
            ParentTokens = new List<SkillTreeToken>();
            IsCollected = false;
        }

        public void SetParentToken(int parent) => ParentTokenIDs.Add(parent);
        public void RemoveParentToken(int parent) => ParentTokenIDs.Remove(parent);

        public void SetChildToken(int child) => ChildTokenIDs.Add(child);
        public void RemoveChildToken(int child) => ChildTokenIDs.Remove(child);

        public void Collect()
        {
            IsCollected = true;
        }

    }
}
