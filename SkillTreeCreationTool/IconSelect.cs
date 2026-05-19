using IdleCollector;
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
    public class IconSelect
    {
        public string iconName;
        public Texture2D icon;

        public Point position;
        public Point size;

        public Button button;

        public IconSelect(string iconName, Texture2D icon, Point size)
        {
            this.iconName = iconName;
            this.icon = icon;

            this.size = size;
            ButtonConfig config = new ButtonConfig();
            config.bounds = new Rectangle(Point.Zero, size);
            config.textures = new Texture2D[] { icon };
            config.useWorldCoord = true;

            button = new Button(Game1.Instance, config);
        }

        public void Update(GameTime gt)
        {
            button.StandardUpdate(gt);
            button.Position = position.ToVector2();
        }

        public void Draw(SpriteBatch sb)
        {
            button.Draw(sb);
        }
    }
}
