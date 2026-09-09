using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace IdleCollector
{
    public abstract class UIContainer
    {
        public Vector2 drawPosition;
        public Spring2D positionSpring;
        public Button button;

        protected List<IRenderable> renderables = new();
        protected Vector2 buttonPosition;
        protected Vector2 outOfScreen = new Vector2(Renderer.ScreenSize.X / 2, -200);

        public void DropIn() => positionSpring.RestPosition = buttonPosition;
        public void DropOut() => positionSpring.RestPosition = outOfScreen;

        public virtual void Draw(SpriteBatch sb)
        {
            foreach (IRenderable renderable in renderables)
                renderable.Draw(sb);
        }

        public abstract void Update(GameTime gameTime);
        public abstract void PrevUpdate(GameTime gameTime);
    }
}
