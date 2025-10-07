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
        public float TailLength;
        public int NumberOfSegments;
        public GetVector TrackPosition;

        public class Trail : IUpdatable, IRenderable
        {
            private Stack<Vector2> tailPoints;
            private TrailInfo info;

            public float LayerDepth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Color Color { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Trail(TrailInfo info)
            {
                this.info = info;
            }

            public void ControlledUpdate(GameTime gameTime)
            {

            }

            public void Draw(SpriteBatch sb)
            {

            }

            public void SlowUpdate(GameTime gameTime)
            {

            }

            public void StandardUpdate(GameTime gameTime)
            {

            }
        }
    }
}
