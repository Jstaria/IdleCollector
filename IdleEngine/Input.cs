using IdleCollector;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public static class Input
    {
        private static MouseState prevMouseState;
        private static MouseState currentMouseState;

        private static KeyboardState prevKBState;
        private static KeyboardState currentKBState;
        private static Keys[] prevKeysPressed;
        private static Keys[] currentKeysPressed;
        private static HashSet<Keys> modifiers = new HashSet<Keys>
        { 
            Keys.LeftAlt, Keys.RightAlt,
            Keys.LeftControl, Keys.RightControl,
            Keys.LeftShift, Keys.RightShift,
        };

        private static Point currentMousePos;
        private static Point prevMousePos;

        private static int currentScroll;
        private static int prevScroll;

        public static void Initialize()
        {
            prevKeysPressed = new Keys[64];
            currentKeysPressed = new Keys[64];

            currentMousePos = new Point(0, 0);
            prevMousePos = new Point(0, 0);

            // Initialize can be found in SceneManager.cs just after Updater Init
            Updater.AddToUpdate(UpdateType.Standard, Update);
        }

        internal static void Update(GameTime gameTime)
        {
            prevMouseState = currentMouseState;
            prevKBState = currentKBState;
            prevKeysPressed = currentKeysPressed;
            prevMousePos = currentMousePos;
            prevScroll = currentScroll;

            currentMouseState = Mouse.GetState();
            currentKBState = Keyboard.GetState();
            
            currentMousePos = currentMouseState.Position;
            Point renderSize = Renderer.RenderSize;
            Point screenSize = Renderer.ScreenSize;
            Point transform = Renderer.CurrentCamera == null ? Point.Zero : Renderer.CurrentCamera.Position;
            currentMousePos.X = (int)(currentMousePos.X * ((float)renderSize.X / (float)screenSize.X));
            currentMousePos.Y = (int)(currentMousePos.Y * ((float)renderSize.Y / (float)screenSize.Y));
            currentMousePos -= transform;

            currentScroll = currentMouseState.ScrollWheelValue;

            currentKeysPressed = currentKBState.GetPressedKeys();
        }

        public static bool IsLeftButtonDownOnce() =>
            (currentMouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released);
        public static bool IsRightButtonDownOnce() =>
            (currentMouseState.RightButton == ButtonState.Pressed && prevMouseState.RightButton == ButtonState.Released);
        public static bool IsMiddleButtonDownOnce() =>
            (currentMouseState.MiddleButton == ButtonState.Pressed && prevMouseState.MiddleButton == ButtonState.Released);
        public static bool IsButtonDownOnce(Keys key) => (currentKBState.IsKeyDown(key) && prevKBState.IsKeyUp(key));
        public static bool AreButtonsDownOnce(params Keys[] keys)
        {
            bool value = true;

            for (int i = 0; i < keys.Length; i++)
            {
                if (modifiers.Contains(keys[i]))
                {
                    if (!IsButtonDown(keys[i]))
                        value = false;
                }
                else
                {
                    if (!IsButtonDownOnce(keys[i]))
                        value = false;
                }
            }

            return value;
        }
        public static bool IsLeftButtonDown() => (currentMouseState.LeftButton == ButtonState.Pressed);
        public static bool IsRightButtonDown() => (currentMouseState.RightButton == ButtonState.Pressed);
        public static bool IsMiddleButtonDown() => (currentMouseState.MiddleButton == ButtonState.Pressed);
        public static bool IsButtonDown(Keys key) => (currentKBState.IsKeyDown(key));
        public static bool AreButtonsDown(params Keys[] keys) => (keys == currentKeysPressed);
        public static Point GetMousePos() => currentMousePos;
        public static Point GetMouseDelta() => prevMousePos - currentMousePos;
        public static int GetMouseScroll() => currentScroll;
        public static int GetMouseScrollDelta() => (int)MathF.Max(-1, MathF.Min(prevScroll - currentScroll, 1));
        private static bool OrderKeys(Keys x, Keys y) => x > y;
        
        
        // SOOOOO much more to come
    }
}
