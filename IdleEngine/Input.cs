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

        public static void Initialize()
        {
            // Initialize can be found in SceneManager.cs just after Updater Init
            Updater.AddToUpdate(UpdateType.Standard, Update);
        }

        internal static void Update(GameTime gameTime)
        {
            prevMouseState = currentMouseState;
            prevKBState = currentKBState;

            currentMouseState = Mouse.GetState();
            currentKBState = Keyboard.GetState();
        }

        public static bool IsLeftButtonDownOnce() =>
            (currentMouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released);
        public static bool IsRightButtonDownOnce() =>
            (currentMouseState.RightButton == ButtonState.Pressed && prevMouseState.RightButton == ButtonState.Released);
        public static bool IsMiddleButtonDownOnce() =>
            (currentMouseState.MiddleButton == ButtonState.Pressed && prevMouseState.MiddleButton == ButtonState.Released);
        public static bool IsButtonDownOnce(Keys key) =>
            (currentKBState.IsKeyDown(key) && prevKBState.IsKeyUp(key));

        // SOOOOO much more to come
    }
}
