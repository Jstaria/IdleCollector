using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public class BezierCurve
    {
        private List<Vector2> points = new();
        public BezierCurve(params Vector2[] points) { this.points.AddRange(points); }

        public void AddPoint(Vector2 point) => points.Add(point);
        public void AddPoints(params Vector2[] points) => this.points.AddRange(points);
        public void InsertPoints(int index, params Vector2[] point) => this.points.InsertRange(index, points);

        public Vector2 GetPointAlongCurve(float indexer)
        {
            indexer = indexer % 1.0f;

            List<Vector2> points = new(this.points); // no need to reverse unless intentional

            Vector2 finalPoint = Vector2.Zero;
            int n = points.Count - 1;

            for (int i = 0; i <= n; i++)
            {
                float coeff = BinomialCoefficient(n, i);
                float weight = coeff * MathF.Pow(1 - indexer, n - i) * MathF.Pow(indexer, i);
                finalPoint += weight * points[i];
            }

            return finalPoint;
        }

        private int BinomialCoefficient(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            if (k == 0 || k == n) return 1;

            int result = 1;
            for (int i = 1; i <= k; i++)
            {
                result *= n - (k - i);
                result /= i;
            }
            return result;
        }

        public void Draw(SpriteBatch sb, int indices, float thickness)
        {
            float delta = 1.0f / (float)indices;

            for (int i = 0; i < indices; i++)
            {
                Vector2 p1 = GetPointAlongCurve(delta * i);
                Vector2 p2 = GetPointAlongCurve(delta * (i + 1));
                Drawing.DrawLine(sb, p1, p2, thickness, Color.Red);
            }

            foreach (Vector2 point in points) 
            {
                int size = 3;
                sb.Draw(ResourceAtlas.GetTexture("square"), new Rectangle((int)point.X - size / 2, (int)point.Y - size / 2, size, size), Color.Blue);
            }
        }
    }
}
