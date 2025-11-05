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
using static System.Net.Mime.MediaTypeNames;

namespace IdleCollector
{
    public delegate void OnButtonClick();
    public delegate void OnButtonClickString(string name);

    public struct ButtonConfig
    {
        public SoundEffect sound;
        public Texture2D[] textures;
        public string font;
        public Rectangle bounds;
        public string[] texts;
        public string textParticle;
        public Color fontColor;
    }

    public class Button : IUpdatable, IRenderable
    {
        private SoundEffect sound;
        private Texture2D[] textures;
        private SpriteFont font;
        private Rectangle bounds;
        private CustomText[] customTexts;
        private string textParticle;
        private Vector2[] textPositions;
        private Color fontColor;
        private float timer = .05f;
        private float timeOfLastPress;
        private bool active;

        public float LayerDepth { get; set; }
        public Color Color { get; set; }

        public event OnButtonClick OnClick;
        public event OnButtonClickString OnClickString;

        /// <summary>
        /// Button Class
        /// </summary>
        public Button(Game gameInstance, ButtonConfig config) : this(gameInstance, config.textures, config.bounds, config.texts, config.textParticle, config.font, config.fontColor, config.sound) { }
        public Button(Game gameInstance, Texture2D[] textures, Rectangle bounds, string[] texts, string textParticle, string fontName, Color fontColor, SoundEffect sound)
        {
            this.textures = textures;
            if (fontName != null)
            {
                this.font = ResourceAtlas.GetFont(fontName);

                Vector2 textLength = font.MeasureString(texts[0]);
                textPositions = new Vector2[2];
                textPositions[0] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );
                textLength = texts.Length == 1 ? textLength : font.MeasureString(texts[1]);
                textPositions[1] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );

                this.customTexts = new CustomText[2];
                this.customTexts[0] = new CustomText(gameInstance, fontName, texts[0], textPositions[0], bounds.Size.ToVector2(), color: fontColor, shadowColor: Color.Black);
                customTexts[0].Refresh();
                this.customTexts[1] = new CustomText(gameInstance, fontName, texts[1], textPositions[1], bounds.Size.ToVector2(), color: fontColor, shadowColor: Color.Black);
                customTexts[1].Refresh();
            }

            this.textParticle = textParticle;
            this.fontColor = fontColor;
            this.bounds = bounds;
            this.sound = sound;

            if (font == null) return;


        }

        public void ControlledUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.TotalGameTime.TotalSeconds - timeOfLastPress;
            active = false;

            if (!bounds.Contains(Input.GetMousePos() * Renderer.UIScaler)) return;

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
            if (textures != null)
            {
                Texture2D texture = !active ? textures[0] : textures[1];
                sb.Draw(texture, bounds, Color.White);
            }

            if (font != null) 
            {
                CustomText text = !active ? customTexts[0] : customTexts[1];
                text.Update(1/60);
                text.Draw();
            }
            
        }

        void IUpdatable.StandardUpdate(GameTime gameTime)
        {

        }

        void IUpdatable.SlowUpdate(GameTime gameTime)
        {

        }
    }
}
