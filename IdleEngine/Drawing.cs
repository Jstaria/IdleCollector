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

        private static Texture2D cachedCircle;

        public static void Initialize(SpriteBatch sb)
        {
            pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            cachedCircle = CreateCircleTexture(sb.GraphicsDevice, 100);
        }

        #region Line

        public static void DrawLine(this SpriteBatch sb, Vector2 point1, Vector2 point2, float thickness, Color color, float layerDepth = 0)
        {
            float distance = Vector2.Distance(point1, point2);
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);

            DrawLine(sb, point1, distance, angle, thickness, color, layerDepth);
        }

        public static void DrawLineCentered(this SpriteBatch sb, Vector2 point1, Vector2 point2, float thickness, Color color, float layerDepth = 0)
        {
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            angle -= MathF.PI / 2;

            Vector2 offset = new(MathF.Cos(angle) * thickness / 2, MathF.Sin(angle) * thickness / 2);

            DrawLine(sb, point1 + offset, point2 + offset, thickness, color, layerDepth);
        }

        public static void DrawLine(this SpriteBatch sb, Vector2 point, float length, float angle, float thickness, Color color, float layerDepth = 0)
        {
            sb.Draw(pixel, point, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, layerDepth);
        }

        #endregion

        #region Shapes
        public static void DrawCircle(this SpriteBatch sb, Vector2 centerPoint, float radius, int divisions, Color color, float layerDepth = 0)
        {
            float angleStep = 2 * MathF.PI / divisions;
            float angle = 0;

            Vector2 edgePos = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius) + centerPoint;

            Vector2 radialPos = new Vector2(MathF.Cos(angle + angleStep) * radius, MathF.Sin(angle + angleStep) * radius);
            float thickness = Vector2.Distance(edgePos, radialPos + centerPoint);

            for (int i = 0; i < divisions; i++)
            {
                DrawLineCentered(sb, centerPoint, edgePos, thickness, color, layerDepth);

                angle += angleStep;

                edgePos = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius) + centerPoint;
            }
        }

        public static void DrawCircle(this SpriteBatch sb, Vector2 centerPoint, float radius, Color color, float layerDepth = 0)
        {
            //if (!cachedCircles.ContainsKey(radius))
            //    cachedCircles.Add(radius, CreateCircleTexture(sb.GraphicsDevice, radius));
            float texRadius = cachedCircle.Width / 2f; 
            float scale = radius / texRadius;          

            Vector2 origin = new Vector2(texRadius);   
            Vector2 drawPos = centerPoint;             

            sb.Draw(cachedCircle, drawPos, null, color, 0f, origin, scale, SpriteEffects.None, layerDepth);
        }

        private static Texture2D CreateCircleTexture(GraphicsDevice graphicsDevice, int radius)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(graphicsDevice, diameter, diameter);

            Color[] data = new Color[diameter * diameter];
            Vector2 center = new Vector2(radius, radius);
            float radiusSquared = radius * radius;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int index = y * diameter + x;
                    Vector2 pos = new Vector2(x, y);
                    float distSq = Vector2.DistanceSquared(pos, center);

                    if (distSq <= radiusSquared)
                        data[index] = Color.White;
                    else
                        data[index] = Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
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
