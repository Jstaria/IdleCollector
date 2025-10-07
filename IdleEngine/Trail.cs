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
        public float SegmentSpawnTime;
        public int NumberOfSegments;
        public GetVector TrackPosition;
        public Curve<Color> SegmentColor;
        public GetFloat TrackLayerDepth;

        public TrailInfo()
        {
            NumberOfSegments = 20;
            TipWidth = 10;
            EndWidth = 1;
            SegmentSpawnTime = .1f;
            TrackPosition = () => { return Vector2.Zero; };
            SegmentColor = (t) => { return Color.White * (t*t); };
            TrackLayerDepth = () => { return 0; };
        }
    }

    public class Trail : IUpdatable, IRenderable
    {
        private Queue<Vector2> trailPoints;
        private TrailInfo info;
        private float spawnTime;

        public float LayerDepth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color Color { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Trail(TrailInfo info)
        {
            this.info = info;
            trailPoints = new Queue<Vector2>();
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            spawnTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (spawnTime < 0)
            {
                if (trailPoints.Count == info.NumberOfSegments)
                    trailPoints.Dequeue();

                trailPoints.Enqueue(info.TrackPosition());
                spawnTime = info.SegmentSpawnTime;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            Vector2[] points = trailPoints.ToArray();

            for (int i = 0; i < points.Length; i++)
            {
                int i2 = (i + 1) % points.Length;
                if (i2 == 0) continue;

                float t = i / (float)points.Length;
                float thickness = MathHelper.Clamp(t * info.TipWidth, info.EndWidth, info.TipWidth);
                sb.DrawCircle(points[i], thickness / 2, 10, info.SegmentColor(t), info.TrackLayerDepth());

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
