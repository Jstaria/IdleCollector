using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public static class Drawing
    {
        private static Texture2D pixel;

        public static void Initialize(SpriteBatch sb)
        {
            pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        #region Line
        public static void DrawLine(this SpriteBatch sb, Vector2 point1, Vector2 point2, float thickness, Color color)
        {
            float distance = Vector2.Distance(point1, point2);
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);

            DrawLine(sb, point1, distance, angle, thickness, color);
        }
        public static void DrawLineCentered(this SpriteBatch sb, Vector2 point1, Vector2 point2, float thickness, Color color)
        {
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            angle -= MathF.PI / 2;

            Vector2 offset = new(MathF.Cos(angle) * thickness / 2, MathF.Sin(angle) * thickness / 2);

            DrawLine(sb, point1 + offset, point2 + offset, thickness, color);
        }
        public static void DrawLine(this SpriteBatch sb, Vector2 point, float length, float angle, float thickness, Color color)
        {
            sb.Draw(pixel, point, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
        }

        public static void DrawRect(this SpriteBatch sb, Rectangle rect, float thickness, Color color)
        {
            Vector2 topLeft = rect.Location.ToVector2();
            Vector2 topRight = (rect.Location + new Point(rect.Width, 0)).ToVector2();
            Vector2 bottomRight = (rect.Location + rect.Size).ToVector2();
            Vector2 bottomLeft = (rect.Location + new Point(0, rect.Height)).ToVector2();

            DrawLine(sb, topLeft, topRight, thickness, color);
            DrawLine(sb, topRight, bottomRight, thickness, color);
            DrawLine(sb, bottomRight, bottomLeft, thickness, color);
            DrawLine(sb, bottomLeft, topLeft, thickness, color);
        }

        #endregion
    }
}
