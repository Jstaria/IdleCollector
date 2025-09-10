using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public delegate void OnButtonClick();
    public delegate void OnButtonClickString(string name);

    public struct ButtonConfig
    {
        public SoundEffect sound;
        public Texture2D[] textures;
        public SpriteFont font;
        public Rectangle bounds;
        public string text;
        public string textParticle;
        public Color fontColor;
    }

    public class Button: IUpdatable, IRenderable
    {
        private SoundEffect sound;
        private Texture2D[] textures;
        private SpriteFont font;
        private Rectangle bounds;
        private UpdateType updateType = UpdateType.Standard;
        private string text;
        private string textParticle;
        private Vector2 textPosition;
        private Color fontColor;
        private float timer = .05f;
        private float timeOfLastPress;
        private bool active;

        public UpdateType Type { get { return updateType; } set { updateType = value; } }

        public event OnButtonClick OnClick;
        public event OnButtonClickString OnClickString;

        /// <summary>
        /// Button Class
        /// </summary>
        public Button(ButtonConfig config) : this(config.textures, config.bounds, config.text, config.textParticle, config.font, config.fontColor, config.sound) { }
        public Button(Texture2D[] textures, Rectangle bounds, string text, string textParticle, SpriteFont font, Color fontColor, SoundEffect sound)
        {
            this.textures = textures;
            this.text = text;
            this.textParticle = textParticle;
            this.font = font;
            this.fontColor = fontColor;
            this.bounds = bounds;
            this.sound = sound;

            if (font == null) return;

            Vector2 textLength = font.MeasureString(text);
            textPosition = new Vector2(
                bounds.X + bounds.Width / 2 - textLength.X / 2,
                bounds.Y + bounds.Height / 2 - textLength.Y / 2
            );
        }

        public void Update(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.TotalGameTime.TotalSeconds - timeOfLastPress;
            active = false;

            if (!bounds.Contains(Input.GetMousePos())) return;
            
            sound?.Play();

            if (timeDelta < timer) return;

            active = true;

            if (Input.IsLeftButtonDownOnce())
            {
                active = false;

                sound?.Play();

                timeOfLastPress = (float)gameTime.TotalGameTime.TotalSeconds;

                OnClick?.Invoke();
                OnClickString?.Invoke(textParticle);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            Texture2D texture = !active ? textures[0] : textures[1];

            sb.Draw(texture, bounds, Color.White);
            
            if (font == null) return;
            sb.DrawString(font, text, textPosition, fontColor);
        }
    }
}
