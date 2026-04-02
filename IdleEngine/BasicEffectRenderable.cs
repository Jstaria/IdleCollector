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
    public class BasicEffectRenderable : IRenderable, ITransform
    {
        private float layerDepth;
        private Color color;
        private Effect effect;
        private Vector2 size;
        private Vector2 position;

        public delegate void OnDraw(Effect effect);
        public event OnDraw DrawEvent;

        public BasicEffectRenderable(Effect effect, Vector2 size, Vector2 position)
        {
            this.effect = effect;
            this.size = size;
            this.position = position;
            this.color = Color.White;
        }

        public float LayerDepth { get => layerDepth; set => layerDepth = value; }
        public Color Color { get => color; set => color = value; }
        public Vector2 Position { get => position; set => position = value; }

        public void Draw(SpriteBatch sb)
        {
            sb.End();
            Renderer.ResetBeginDrawEffect(sb, effect);

            DrawEvent?.Invoke(effect);

            sb.Draw(Drawing.Pixel, position, null, color, 0, Vector2.Zero, size, SpriteEffects.None, layerDepth);

            sb.End();
            Renderer.ResetBeginDraw(sb);
        }

        public void Move(Vector2 direction) => position += direction;

        public void MoveTo(Vector2 position) => this.position = position;
    }
}
