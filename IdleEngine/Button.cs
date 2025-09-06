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
    // Called by reference when button is clicked on
    public delegate void OnRightButtonClick();
    public delegate void OnLeftButtonClick();
    public delegate void OnButtonClickString(string name);

    public class Button
    {
        private Rectangle position;
        private Vector2 textPos;
        private Texture2D[] assets;
        private MouseState prevMState;
        private KeyboardState prevKBState;
        private string text;
        private SpriteFont font;
        private Color fontColor;
        private Color buttonColorNotActive;
        private Color buttonColorActive;
        private bool active;
        private float expandNum = 10;
        private int expandNumHalf;
        private bool expand;
        private float timer;
        private float timeSinceLastPress;
        private bool switchBool;
        private bool isSwitch;
        private SoundEffectInstance sound;

        public Rectangle Position { get { return position; } }
        public string HoverText { get; set; }
        public bool Active { get { return active; } }
        public bool SwitchBool { get { return switchBool; } set { switchBool = value; } }

        public event OnRightButtonClick OnRightClick;
        public event OnLeftButtonClick OnLeftClick;
        public event OnButtonClickString OnLeftClickString;
        public event OnButtonClickString OnRightClickString;

        /// <summary>
        /// Button Class
        /// </summary>
        /// <param name="assets">First asset is non-active texture, Second is when hovered over</param>
        /// <param name="position"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="fontColor"></param>
        public Button(Texture2D[] assets, Rectangle position, string text, SpriteFont font, Color fontColor, Color buttonColor, float timer, SoundEffect sound)
        {
            this.timer = timer;
            this.assets = assets;
            this.text = text;
            this.font = font;
            this.fontColor = fontColor;
            buttonColorNotActive = buttonColor;
            buttonColorActive = buttonColor;
            this.position = position;
            expandNumHalf = (int)expandNum / 2;

            Vector2 textLength = font.MeasureString(text);
            textPos = new Vector2(
                position.X + position.Width / 2 - textLength.X / 2,
                position.Y + position.Height / 2 - textLength.Y / 2
            );

            this.sound = sound.CreateInstance();
            this.sound.Volume = .05f;
        }

        public Button(Texture2D[] assets, Rectangle position, string text, SpriteFont font, Color fontColor, Color buttonColorNotActive, Color buttonColorActive, float timer, SoundEffect sound)
        {
            this.timer = timer;
            this.assets = assets;
            this.text = text;
            this.font = font;
            this.fontColor = fontColor;
            this.buttonColorNotActive = buttonColorNotActive;
            this.buttonColorActive = buttonColorActive;
            this.position = position;
            expandNumHalf = (int)expandNum / 2;

            Vector2 textLength = font.MeasureString(text);
            textPos = new Vector2(
                position.X + position.Width / 2 - textLength.X / 2,
                position.Y + position.Height / 2 - textLength.Y / 2
            );

            this.sound = sound.CreateInstance();
            this.sound.Volume = .05f;
        }

        public Button(Texture2D[] assets, Rectangle position, string text, SpriteFont font, Color fontColor, Color buttonColorNotActive, Color buttonColorActive, float timer, bool isSwitch, SoundEffect sound)
        {
            this.timer = timer;
            this.assets = assets;
            this.text = text;
            this.font = font;
            this.fontColor = fontColor;
            this.buttonColorNotActive = buttonColorNotActive;
            this.buttonColorActive = buttonColorActive;
            this.position = position;
            expandNumHalf = (int)expandNum / 2;

            Vector2 textLength = font.MeasureString(text);
            textPos = new Vector2(
                position.X + position.Width / 2 - textLength.X / 2,
                position.Y + position.Height / 2 - textLength.Y / 2
            );
            this.isSwitch = isSwitch;

            this.sound = sound.CreateInstance();
            this.sound.Volume = .05f;
        }

        public void Update(GameTime gameTime)
        {
            expandNum = 10;
            expand = false;

            bool canPress = false;

            active = false;

            MouseState currentMState = Mouse.GetState();
            KeyboardState currentKBState = Keyboard.GetState();

            // If the cursor is over the button, it will trigger a different texture
            // and allow it to be clicked
            if (position.Contains(currentMState.Position))
            {
                active = true;
            }

            if (gameTime.TotalGameTime.TotalSeconds - timeSinceLastPress >= timer) { canPress = true; }

            if ((currentMState.LeftButton == ButtonState.Pressed &&
                prevMState.LeftButton == ButtonState.Released
                || currentKBState.IsKeyDown(Keys.C) &&
                prevKBState.IsKeyUp(Keys.C)) &&
                active && canPress)
            {
                sound.Play();

                timeSinceLastPress = (float)gameTime.TotalGameTime.TotalSeconds;

                if (OnLeftClick != null)
                {
                    OnLeftClick();
                    expand = true;
                }

                if (OnLeftClickString != null)
                {
                    OnLeftClickString(HoverText);
                    expand = true;
                }
            }

            if (currentMState.RightButton == ButtonState.Pressed &&
                prevMState.RightButton == ButtonState.Released &&
                active && canPress)
            {
                sound.Play();

                timeSinceLastPress = (float)gameTime.TotalGameTime.TotalSeconds;

                if (OnRightClick != null)
                {
                    OnRightClick();
                    expand = true;
                }

                if (OnRightClickString != null)
                {
                    OnRightClickString(text);
                    expand = true;
                }
            }

            if (expand)
            {
                if (expandNum > expandNumHalf)
                {
                    expandNum = expandNumHalf;
                }
                else if (expandNum <= expandNumHalf * 2)
                {
                    expandNum *= 1.1f;
                }
            }

            prevKBState = currentKBState;
            prevMState = currentMState;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!isSwitch)
            {
                // Hovered over
                if (active)
                {
                    sb.Draw(assets[1], new Rectangle((int)(position.X - expandNum), (int)(position.Y - expandNum), (int)(position.Width + expandNum * 2), (int)(position.Height + expandNum * 2)), buttonColorActive);

                    if (HoverText != null)
                    {
                        Vector2 textLength = font.MeasureString(HoverText);
                        Vector2 textPos = new Vector2(
                            Mouse.GetState().X - textLength.X / 2,
                            Mouse.GetState().Y - textLength.Y
                            );

                        sb.DrawString(font, HoverText, textPos, Color.Black);
                    }
                }
                // Regular
                else
                {
                    sb.Draw(assets[0], position, buttonColorNotActive);
                }
            }

            else
            {
                Color color = switchBool ? buttonColorActive : buttonColorNotActive;

                sb.Draw(assets[0], position, color);
            }


            if (text != null)
            {
                // Draw button text over the button
                sb.DrawString(font, text, textPos, fontColor);
            }
        }
    }
}
