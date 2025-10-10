using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class ResourceUIObject: IRenderable
    {
        private float totalTime;
        private float timer;
        private float despawnTimer = .25f;
        private float t;
        private Trail objectTrail;
        private Vector2 position;
        private BezierCurve path;

        public ResourceInfo ResourceInfo { get; private set; }  

        public delegate void Despawn(ResourceUIObject obj);
        private Despawn OnDespawn;

        public ResourceUIObject(float timeToUI, ResourceInfo resInfo, Vector2 pos, Vector2 endPos, float layerDepth, Despawn despawn)
        {
            ResourceInfo = resInfo;
            OnDespawn = despawn;
            totalTime = timeToUI;
            timer = 0f;
            position = pos;
            this.LayerDepth = layerDepth;
            CreatePath(pos, endPos);
            CreateTrail(resInfo);
        }

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public void Update(GameTime gameTime)
        {
            float tMax = .999f;
            t = MathHelper.Clamp(timer / totalTime, 0, tMax);

            position = path.GetPointAlongCurve(t);
            objectTrail.ControlledUpdate(gameTime);

            if (t == tMax)
            {
                if ((despawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds) < 0)
                    OnDespawn(this);
            }

            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Draw(SpriteBatch sb)
        {
            objectTrail.Draw(sb);
        }

        private void CreatePath(Vector2 pos, Vector2 endPos)
        {
            path = new BezierCurve();

            Vector2 randomDirection = Vector2.Normalize(RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One));
            Vector2 randomPosition = pos + randomDirection * 500;

            path.AddPoints(
                pos,
                randomPosition,
                endPos
                );
        }

        private void CreateTrail(ResourceInfo resInfo)
        {
            TrailInfo info = new TrailInfo();

            info.SegmentColor = (t) => Color.Lerp(Color.Yellow * t * t, Color.Red * t * t, this.t);
            info.TrackLayerDepth = () => LayerDepth;
            info.TrackPosition = () => position;
            info.NumberOfSegments = 100;
            info.TrailLength = 500;
            info.TipWidth = 16;
            info.EndWidth = 0;
            info.SegmentsPerSecond = (float)info.NumberOfSegments * .5f;
            info.SegmentsRemovedPerSecond = info.SegmentsPerSecond * .5f;

            objectTrail = new Trail(info);
        }
    }
}
