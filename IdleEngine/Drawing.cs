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
        private static SpriteBatch _spriteBatch;

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

        #endregion
    }
}
