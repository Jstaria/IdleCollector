using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public struct TrailInfo
    {
        public int TipWidth;
        public int EndWidth;
        public float TrailLength;
        public int NumberOfSegments;
        public GetVector TrackPosition;
        public Curve<Color> SegmentColor;
        public GetFloat TrackLayerDepth;
        public float SegmentsPerSecond;
        public float SegmentsRemovedPerSecond;

        public TrailInfo()
        {
            NumberOfSegments = 20;
            SegmentsPerSecond = 12;
            SegmentsRemovedPerSecond = 10;
            TipWidth = 10;
            EndWidth = 1;
            TrailLength = 100f;
            TrackPosition = () => { return Vector2.Zero; };
            SegmentColor = (t) => { return Color.White * (t*t); };
            TrackLayerDepth = () => { return 0; };
        }
    }

    public class Trail : IUpdatable, IRenderable
    {
        private Queue<Vector2> trailPoints;
        private TrailInfo info;
        private float removeCount;
        private float addCount;

        public float LayerDepth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color Color { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Trail(TrailInfo info)
        {
            this.info = info;
            trailPoints = new Queue<Vector2>();
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (trailPoints.Count == 0)
                trailPoints.Enqueue(info.TrackPosition());
            else
            {
                Vector2[] points = trailPoints.ToArray();

                if (points.Length > 0 && points[0] != info.TrackPosition())
                {
                    while (addCount >= 1f)
                    {
                        addCount--;
                        trailPoints.Enqueue(info.TrackPosition());
                    }

                    addCount += info.SegmentsPerSecond * dt;
                    addCount = MathF.Min(addCount, info.SegmentsPerSecond);
                }

                float distance = 0;

                if (points.Length <= 1) return;

                int dequeueAmt = 0;

                for (int i = 0; i < points.Length; i++)
                {
                    if ((i + 1) % points.Length == 0) break;

                    distance += Vector2.Distance(points[i], points[i + 1]);
                    if (distance > info.TrailLength)
                    {
                        dequeueAmt = points.Length - i;
                        break;
                    }
                }

                for (int i = 0; i < dequeueAmt; i++)
                {
                    trailPoints.Dequeue();
                }
            }

            while (removeCount >= 1f && trailPoints.Count > 0)
            {
                removeCount--;
                trailPoints.Dequeue();
            }

            if (trailPoints.Count > 0)
            {
                removeCount += info.SegmentsRemovedPerSecond * dt;
                removeCount = MathF.Min(removeCount, info.SegmentsRemovedPerSecond);
            }
            
        }

        public void Draw(SpriteBatch sb)
        {
            Vector2[] points = trailPoints.ToArray();

            for (int i = 0; i < points.Length; i++)
            {
                int i2 = (i + 1) % points.Length;

                float t = i / (float)points.Length;
                float thickness = MathHelper.Clamp(t * info.TipWidth, info.EndWidth, info.TipWidth);
                sb.DrawCircle(points[i], thickness / 2, info.SegmentColor(t), info.TrackLayerDepth());
                
                if (i2 == 0) continue;
                
                sb.DrawLineCentered(points[i], points[i2], thickness, info.SegmentColor(t), info.TrackLayerDepth());
            }
        }

        public void SlowUpdate(GameTime gameTime)
        {

        }

        public void StandardUpdate(GameTime gameTime)
        {

        }
    }
}
