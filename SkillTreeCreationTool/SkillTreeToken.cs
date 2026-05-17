using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public List<int> ParentTokens { get; set; }

        public bool IsCollected { get; set; }
        public bool IsCollectable {  get; set; }

        public SkillTreeToken(string iconName, Point gridPosition, int tokenID)
        {
            TokenIcon = iconName;
            TokenID = tokenID;
            GridPosition = gridPosition;
            ParentTokens = new List<int>();
            IsCollected = false;
        }

        public void SetParentToken(int parent)
        {
            ParentTokens.Add(parent);
        }
    }
}
