using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        public Color shadowColor;
        public float rotationRadians;
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
        private float rotationRadians;
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
        public Button(Game gameInstance, ButtonConfig config) : this(gameInstance, config.textures, config.bounds, config.texts, config.textParticle, config.font, config.fontColor, config.shadowColor, config.sound, config.rotationRadians) { }
        public Button(Game gameInstance, Texture2D[] textures, Rectangle bounds, string[] texts, string textParticle, string fontName, Color fontColor, Color shadowColor, SoundEffect sound, float rotationRadians)
        {
            this.rotationRadians = rotationRadians;
            this.textures = textures;
            if (fontName != null)
            {
                string[] tempTexts = new string[texts.Length]; 

                Regex regex = new(@"<fx\b[^>]*>(.*?)</fx>", RegexOptions.Singleline);
                tempTexts[0] = regex.Replace(texts[0], m => m.Groups[0].Value);
                tempTexts[1] = regex.Replace(texts[1], m => m.Groups[1].Value);

                this.font = ResourceAtlas.GetFont(fontName);

                Vector2 textLength = font.MeasureString(tempTexts[0]);
                textPositions = new Vector2[2];
                textPositions[0] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );
                textLength = texts.Length == 1 ? textLength : font.MeasureString(tempTexts[1]);
                textPositions[1] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );

                this.customTexts = new CustomText[2];
                this.customTexts[0] = new CustomText(gameInstance, "Fonts/" + fontName, texts[0], textPositions[0], bounds.Size.ToVector2(), color: fontColor, shadowColor: shadowColor);
                customTexts[0].Refresh();
                this.customTexts[1] = new CustomText(gameInstance, "Fonts/" + fontName, texts[1], textPositions[1], bounds.Size.ToVector2(), color: fontColor, shadowColor: shadowColor);
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
            
        }

        public void Draw(SpriteBatch sb)
        {
            RenderTarget2D target = new RenderTarget2D(sb.GraphicsDevice, bounds.Size.X, bounds.Size.Y);

            //if (rotationRadians > 0)
            //{
            //    sb.End();
            //    sb.GraphicsDevice.SetRenderTarget(target);
            //    sb.GraphicsDevice.Clear(Color.White);
            //    Renderer.ResetBeginDraw(sb);
            //}

            if (textures != null)
            {
                Texture2D texture = !active ? textures[0] : textures[1];
                sb.Draw(texture, bounds, Color.White);
            }

            if (customTexts != null) 
            {
                CustomText text = !active ? customTexts[0] : customTexts[1];
                text.Update(1/60.0f);
                text.Draw();
            }

            //if (rotationRadians > 0)
            //{
            //    sb.End();
            //    Renderer.ResetRenderTarget(sb);
            //    sb.GraphicsDevice.Clear(Color.CornflowerBlue);
            //    Renderer.ResetBeginDraw(sb);
            //    sb.Draw(target, new Rectangle(new Point(100,-100), bounds.Size), null, Color.White, rotationRadians, -bounds.Center.ToVector2(), SpriteEffects.None, 0);
            //    target.Dispose();
            //}
        }

        public void StandardUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.TotalGameTime.TotalSeconds - timeOfLastPress;
            active = false;

            //if (rotationRadians > 0)
            //{
            //    if (!CollisionHelper.GetRotRectIntersect(bounds, rotationRadians, Input.GetMouseScreenPos().ToVector2())) return;
            //}
            //else
            //{
                if (!bounds.Contains(Input.GetMousePos() * Renderer.UIScaler)) return;
            //}

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

        void IUpdatable.SlowUpdate(GameTime gameTime)
        {

        }
    }
}
