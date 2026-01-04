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
using System.Xml.Linq;
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
        public Vector2 textOffset;
        public string[] texts;
        public string textParticle;
        public Color fontColor;
        public Color shadowColor;
        public float rotationRadians;
        public OnButtonClick OnClick;
        public OnButtonClickString OnClickString;
    }

    public class Button : IUpdatable, IRenderable
    {
        private SoundEffect sound;
        private Texture2D[] textures;
        private SpriteFont font;
        private Rectangle bounds;
        private string[] texts;
        private CustomText[] customTexts;
        private string textParticle;
        private Vector2[] textPositions;
        private Color fontColor;
        private float rotationRadians;
        private float timer = .05f;
        private float timeOfLastPress;
        private bool active;
        private RenderTarget2D[] targets;
        public float LayerDepth { get; set; }
        public Color Color { get; set; }
        public Vector2 Position
        {
            get => bounds.Location.ToVector2(); set
            {
                bounds.Location = value.ToPoint();
                if (texts != null)
                    UpdateTextPositions();
            }
        }

        public event OnButtonClick OnClick;
        public event OnButtonClickString OnClickString;

        /// <summary>
        /// Button Class
        /// </summary>
        public Button(Game gameInstance, ButtonConfig config) : this(gameInstance, config.textures, config.bounds, config.texts, config.textParticle, config.font, config.fontColor, config.shadowColor, config.sound, config.rotationRadians, config) { }
        public Button(Game gameInstance, Texture2D[] textures, Rectangle bounds, string[] texts, string textParticle, string fontName, Color fontColor, Color shadowColor, SoundEffect sound, float rotationRadians, ButtonConfig config)
        {
            OnClick = config.OnClick;
            OnClickString = config.OnClickString;

            this.rotationRadians = rotationRadians;
            this.textures = textures;
            if (fontName != null)
            {
                this.texts = new string[texts.Length];

                Regex regex = new(@"<fx\b[^>]*>(.*?)</fx>", RegexOptions.Singleline);
                this.texts[0] = regex.Replace(texts[0], m => m.Groups[0].Value);
                this.texts[1] = regex.Replace(texts[1], m => m.Groups[1].Value);

                this.font = ResourceAtlas.GetFont(fontName);

                Vector2 textLength = font.MeasureString(this.texts[0]);
                textPositions = new Vector2[2];
                textPositions[0] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );
                textLength = texts.Length == 1 ? textLength : font.MeasureString(this.texts[1]);
                textPositions[1] = new Vector2(
                    bounds.X + bounds.Width / 2 - textLength.X / 2,
                    bounds.Y + bounds.Height / 2 - textLength.Y / 2
                );

                this.customTexts = new CustomText[2];
                this.customTexts[0] = new CustomText(gameInstance, "Fonts/" + fontName, texts[0], textPositions[0], bounds.Size.ToVector2(), color: fontColor, shadowColor: shadowColor, offset: config.textOffset);
                customTexts[0].Refresh();
                this.customTexts[1] = new CustomText(gameInstance, "Fonts/" + fontName, texts[1], textPositions[1], bounds.Size.ToVector2(), color: fontColor, shadowColor: shadowColor, offset: config.textOffset);
                customTexts[1].Refresh();
            }

            this.textParticle = textParticle;
            this.fontColor = fontColor;
            this.bounds = bounds;
            this.sound = sound;

            if (rotationRadians != 0)
                Init(gameInstance);
        }

        public void Draw(SpriteBatch sb)
        {
            if (rotationRadians != 0)
            {
                RenderTarget2D target = active ? targets[0] : targets[1];

                sb.Draw(target, bounds, null, Color.White, rotationRadians, bounds.Size.ToVector2() / 2/*Vector2.Zero*/, SpriteEffects.None, 0);
            }
            else
            {
                if (textures != null)
                {
                    int i = textures.Length == 1 ? 0 : !active ? 0 : 1;
                    Texture2D texture = textures[i];
                    sb.Draw(texture, bounds, Color.White);
                }

                if (customTexts != null)
                {
                    int i = customTexts.Length == 1 ? 0 : !active ? 0 : 1;
                    CustomText text = customTexts[i];
                    text.Update(1 / 60.0f);
                    text.Draw();
                }
            }

        }

        public void DrawRotated(SpriteBatch sb)
        {
            RenderTarget2D target = active ? targets[0] : targets[1];
            sb.GraphicsDevice.SetRenderTarget(target);
            sb.GraphicsDevice.Clear(Color.Transparent);
            Renderer.ResetBeginDraw(sb);

            if (textures != null)
            {
                Texture2D texture = textures.Length == 1 ? textures[0] : !active ? textures[0] : textures[1];
                sb.Draw(texture, new Rectangle(Point.Zero, bounds.Size), Color.White);
            }

            if (customTexts != null)
            {
                int i = customTexts.Length == 1 ? 0 : !active ? 0 : 1;

                CustomText text = customTexts[i];
                text.Update(1 / 60.0f);

                Vector2 tempPosition = new Vector2(text.Position.X, text.Position.Y);

                text.Position = textPositions[i] - bounds.Location.ToVector2();
                text.Draw();
                text.Position = tempPosition;
            }

            sb.End();
            sb.GraphicsDevice.SetRenderTarget(null);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.TotalGameTime.TotalSeconds - timeOfLastPress;
            active = false;

            if (rotationRadians != 0)
            {
                if (!CollisionHelper.GetRotRectIntersect(bounds, rotationRadians, (Input.GetMouseScreenPos() * Renderer.UIScaler).ToVector2(), -bounds.Size.ToVector2() / 2)) return;
            }
            else
            {
                if (!bounds.Contains(Input.GetMouseScreenPos() * Renderer.UIScaler)) return;
            }

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
        void IUpdatable.ControlledUpdate(GameTime gameTime)
        {

        }

        void IUpdatable.SlowUpdate(GameTime gameTime)
        {

        }

        private void Init(Game gameInst)
        {
            targets = [
                new RenderTarget2D(gameInst.GraphicsDevice, bounds.Size.X, bounds.Size.Y),
                new RenderTarget2D(gameInst.GraphicsDevice, bounds.Size.X, bounds.Size.Y)
            ];

            Renderer.AddToDrawRT(DrawRotated);
        }

        private void UpdateTextPositions()
        {
            Vector2 textLength = font.MeasureString(this.texts[0]);
            textPositions = new Vector2[2];
            textPositions[0] = new Vector2(
                bounds.X + bounds.Width / 2 - textLength.X / 2,
                bounds.Y + bounds.Height / 2 - textLength.Y / 2
            );
            textLength = texts.Length == 1 ? textLength : font.MeasureString(this.texts[1]);
            textPositions[1] = new Vector2(
                bounds.X + bounds.Width / 2 - textLength.X / 2,
                bounds.Y + bounds.Height / 2 - textLength.Y / 2
            );
        }
    }
}
