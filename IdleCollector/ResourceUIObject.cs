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
        private float randomModifier;
        public ResourceInfo ResourceInfo { get; private set; }  

        public delegate void Despawn(ResourceUIObject obj);
        private Despawn OnDespawn;

        public float T { get => t; }

        public ResourceUIObject(float timeToUI, ResourceInfo resInfo, Vector2 pos, Vector2 endPos, float layerDepth, Despawn despawn, float randomModifier = 1)
        {
            ResourceInfo = resInfo;
            OnDespawn = despawn;
            totalTime = timeToUI;
            timer = 0f;
            position = pos;
            this.LayerDepth = layerDepth;
            this.randomModifier = randomModifier;
            CreatePath(pos, endPos);
        }

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public void Update(GameTime gameTime)
        {
            float tMax = .999f;
            t = MathHelper.Clamp(timer / totalTime, 0, tMax);
            if (objectTrail == null) OnDespawn(this);

                position = path.GetPointAlongCurve(t);
            objectTrail?.ControlledUpdate(gameTime);

            if (t == tMax)
            {
                if ((despawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds) < 0)
                    OnDespawn(this);
            }

            timer += (float)gameTime.ElapsedGameTime.TotalSeconds * randomModifier;
        }

        public void Draw(SpriteBatch sb)
        {
            objectTrail?.Draw(sb);
        }

        private void CreatePath(Vector2 pos, Vector2 endPos)
        {
            path = new BezierCurve();

            Vector2 randomDirection = Vector2.Normalize(RandomHelper.Instance.GetVector2(-Vector2.One, Vector2.One));
            Vector2 randomPosition = pos + randomDirection * RandomHelper.Instance.GetFloat(250, 750);

            path.AddPoints(
                pos,
                randomPosition,
                endPos
                );
        }

        public void CreateTrail(TrailInfo info)
        {
            info.TrackPosition = () => position;
            objectTrail = new Trail(info);
        }
    }
}
