using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace IdleCollector
{
    public class Slider : UIContainer
    {
        private int min = 0, max = 0, value = 0;
        private int sliderWidth = 280, sliderStartX = 0, sliderEndX = 0;
        private int barWidth;
        private const float SegmentGap = 4f;
        private float segmentWidth;
        private float TrackWidth => sliderWidth;
        private int sensitivity = 50;
        private readonly int divisions;

        public delegate int OnSlide(float value);
        public delegate int GetValue();

        private OnSlide slide;
        private ButtonConfig config;
        private Vector2 textOffset;
        private Texture2D barTex;

        private int ScaledMouseX => (int)(Input.GetMouseScreenPos().X /*+ (barWidth / 4) * button.ScaleSpring.Position * 2*/);

        public Slider(ButtonConfig config, OnSlide slide, GetValue getValue, int divisions)
        {
            if (divisions <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisions), "Slider divisions must be greater than zero.");

            this.divisions = divisions;
            segmentWidth = (TrackWidth - SegmentGap * (divisions - 1)) / divisions;
            if (segmentWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisions), "Slider divisions leave no room for segments.");

            barTex = ResourceAtlas.GetTexture("bar1");
            config.OnClick = GetMouseInput;
            config.bounds.Height = 20 * Renderer.UIScaler.X;
            config.OnDrawButton = DrawSlider;

            ButtonConfig config2 = config;
            textOffset = -new Vector2(config.bounds.Size.X / 4, 0);
            config2.textOffset = textOffset;
            config2.rotationRadians = 0.001f;
            config2.textures = new[] { ResourceAtlas.GetTexture("board" + RandomHelper.Instance.GetInt(5, 8)) };

            this.slide = slide;
            this.config = config;
            button = new Button(Game1.Instance, config2);
            buttonPosition = config.bounds.Location.ToVector2();
            outOfScreen = new Vector2(buttonPosition.X, -200);
            positionSpring = new Spring2D(20, .65f, outOfScreen);
            button.Position = outOfScreen;
            barWidth = this.sliderWidth / MenuData.divisions;
            drawPosition = positionSpring.Position;
            sliderStartX = (int)drawPosition.X;
            sliderEndX = sliderStartX + (int)(TrackWidth / Renderer.UIScaler.X);

            sliderStartX /= Renderer.UIScaler.X;
            sliderEndX /= Renderer.UIScaler.Y;

            renderables.Add(button);
            GetValueWait(100, getValue);
        }

        private async void GetValueWait(int time, GetValue getValue)
        {
            await Task.Delay(time);
            value = getValue.Invoke();
        }

        public override void PrevUpdate(GameTime gameTime)
        {
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
            UpdateSliderBounds();
        }

        public override void Update(GameTime gameTime)
        {
            button.StandardUpdate(gameTime);
            positionSpring.Update();
            drawPosition = positionSpring.Position;
            button.Position = drawPosition;
            UpdateSliderBounds();
        }

        private void DrawSlider(SpriteBatch sb)
        {
            Vector2 pos = new Vector2(config.bounds.Width / 2, config.bounds.Height / 4);
            sensitivity = (int)(segmentWidth * .75f);

            float startX = config.bounds.Width / 2f;
            for (int i = 0; i < divisions; i++)
            {
                Color color = i >= value ? new Color(30, 15, 15) : Color.White;
                int left = (int)MathF.Round(startX);
                int right = (int)MathF.Round(startX + segmentWidth);
                int width = Math.Max(right - left, 1);

                sb.Draw(barTex, new Rectangle(left - 4, (int)pos.Y + 4, width, barTex.Height * Renderer.UIScaler.Y), null, Color.Black * .25f, 0, Vector2.Zero, SpriteEffects.None, 0f);
                sb.Draw(barTex, new Rectangle(left, (int)pos.Y, width, barTex.Height * Renderer.UIScaler.Y), null, color, 0, Vector2.Zero, SpriteEffects.None, .01f);
                startX += segmentWidth + SegmentGap;
            }
        }

        private async void GetMouseInput()
        {
            int mouseX = ScaledMouseX;
            if (mouseX < sliderStartX || mouseX > sliderEndX)
                return;

            while (Input.IsLeftButtonDown())
            {
                float xDistance = MathHelper.Max(mouseX - sliderStartX, 0);
                float totalSliderWidth = sliderEndX - sliderStartX;
                float trackPosition = MathHelper.Clamp(xDistance / totalSliderWidth * TrackWidth, 0, TrackWidth);
                float ratio = GetDivisionCount(trackPosition) / (float)divisions;

                mouseX = ScaledMouseX;
                value = slide.Invoke(ratio);

                await Task.Delay(1);
            }
        }

        private int GetDivisionCount(float trackPosition)
        {
            float segmentCenter = segmentWidth / 2f;
            if (trackPosition < segmentCenter)
                return 0;

            float divisionStride = segmentWidth + SegmentGap;
            int count = 1 + (int)MathF.Floor((trackPosition - segmentCenter + divisionStride / 2f) / divisionStride);
            return Math.Clamp(count, 0, divisions);
        }

        private void UpdateSliderBounds()
        {
            sliderEndX = sliderStartX + (int)(TrackWidth / Renderer.UIScaler.X * button.ScaleSpring.Position);
        }
    }
}
