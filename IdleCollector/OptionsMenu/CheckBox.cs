using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IdleCollector
{
    public class CheckBox : UIContainer
    {
        public delegate bool OnCheck(bool value);
        public delegate bool GetValue();

        private bool value;
        private ButtonConfig config;
        private Vector2 textOffset;
        private Texture2D boxTex;
        private Texture2D checkTex;
        private Texture2D shadowTex;

        public event OnCheck OnBoxCheck;

        public CheckBox(ButtonConfig config, OnCheck onCheck, GetValue getValue)
        {
            checkTex = ResourceAtlas.GetTexture("check16x16");
            boxTex = ResourceAtlas.GetTexture("checkboxThin");
            shadowTex = ResourceAtlas.GetTexture("dropShadow");
            OnBoxCheck = onCheck;

            config.bounds.Height = 20 * Renderer.UIScaler.X;
            config.OnClick += () => { value = OnBoxCheck.Invoke(!value); };
            config.OnDrawButton = DrawCheckbox;

            ButtonConfig config2 = config;
            textOffset = -new Vector2(config.bounds.Size.X / 4, 0);
            config2.textOffset = textOffset;
            config2.rotationRadians = 0.001f;
            config2.textures = new[] { ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(5, 8)) };

            this.config = config;
            button = new Button(Game1.Instance, config2);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;
            drawPosition = positionSpring.Position;
            value = getValue.Invoke();

            renderables.Add(button);
        }

        public override void PrevUpdate(GameTime gameTime)
        {
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }

        public override void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
        }

        private void DrawCheckbox(SpriteBatch sb)
        {
            int size = boxTex.Width * Renderer.UIScaler.X;
            Vector2 center = new Vector2(config.bounds.Width * .75f - boxTex.Width / 2, config.bounds.Height / 2);

            Rectangle drawRect = new Rectangle(
                (center - new Vector2(size * 0.5f)).ToPoint(),
                new Point(size));

            Rectangle shadowRect = new Rectangle(
                drawRect.Location + new Point(-6, 6),
                drawRect.Size);

            sb.Draw(boxTex, shadowRect, Color.Black * 0.25f);
            sb.Draw(boxTex, drawRect, Color.White);

            if (value)
            {
                sb.Draw(shadowTex, drawRect, Color.White);
                sb.Draw(checkTex, shadowRect, Color.Black * 0.25f);
                sb.Draw(checkTex, drawRect, Color.White);
            }
        }
    }
}
