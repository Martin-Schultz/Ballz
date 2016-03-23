using Ballz.GameSession.Logic;
using Ballz.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;
using System;
using System.Collections.Generic;

namespace Ballz.Input
{
    /// <summary>
    ///     Input translator takes care of all physical inputs with regards to specified keymappings etc.
    ///     It translates these inputs to corresponding Game Messages.
    /// </summary>
    public class InputTranslator : GameComponent
    {
        private bool subscribed = false;
        
        private KeyboardState previousState;
        private KeyboardState currentState;
        private GamePadState[] previousGamePadState;
        private PlayerIndex[] gamePadPlayerIndex;
        
        public event EventHandler<InputMessage> GameInput;

        public InputTranslator(Ballz game) : base(game)
        {
            previousState = Keyboard.GetState();
            currentState = previousState;
            previousGamePadState = new GamePadState[4];

            TextInputEXT.TextInput += ProcessTextInput;
            TextInputEXT.StartTextInput();

            gamePadPlayerIndex = new PlayerIndex[4];
            gamePadPlayerIndex[0] = PlayerIndex.One;
            gamePadPlayerIndex[1] = PlayerIndex.Two;
            gamePadPlayerIndex[2] = PlayerIndex.Three;
            gamePadPlayerIndex[3] = PlayerIndex.Four;
            //previousGamePadState = GamePad.GetState(
        }
        
        private void OnInput(InputMessage.ControlButton inputMessage, bool? pressed = null, char? key = null, Player player = null)
        {
            GameInput?.Invoke(this, new InputMessage(inputMessage, pressed, key, player)); //todo: use object pooling and specify message better
        }

        private void ProcessGamePadInput(int p)
        {       
            int sign = 1;
            GamePadState currentState = GamePad.GetState(gamePadPlayerIndex[p-1]);
            if (System.Environment.OSVersion.Platform != PlatformID.Unix || System.Environment.OSVersion.Platform != PlatformID.MacOSX)
                sign = -1;
            if (currentState.IsConnected)
            {                                    
                if (previousGamePadState[p - 1].DPad.Up == ButtonState.Released && currentState.DPad.Up == ButtonState.Pressed)
                    OnInput(InputMessage.ControlButton.ControlsUp, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Down == ButtonState.Released && currentState.DPad.Down == ButtonState.Pressed)
                    OnInput(InputMessage.ControlButton.ControlsDown, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Left == ButtonState.Released && currentState.DPad.Left == ButtonState.Pressed)
                    OnInput(InputMessage.ControlButton.ControlsLeft, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Right == ButtonState.Released && currentState.DPad.Right == ButtonState.Pressed)
                    OnInput(InputMessage.ControlButton.ControlsRight, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonUp(Buttons.B) && currentState.IsButtonDown(Buttons.B))
                    OnInput(InputMessage.ControlButton.ControlsBack, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonUp(Buttons.A) && currentState.IsButtonDown(Buttons.A))
                    OnInput(InputMessage.ControlButton.ControlsAction, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonUp(Buttons.X) && currentState.IsButtonDown(Buttons.X))
                    OnInput(InputMessage.ControlButton.ControlsJump, true, null, Ballz.The().Match?.PlayerByNumber(p));

                if(previousGamePadState[p - 1].ThumbSticks.Left.X <= 0.5 && currentState.ThumbSticks.Left.X > 0.5)
                    OnInput(InputMessage.ControlButton.ControlsRight, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.X >= -0.5 && currentState.ThumbSticks.Left.X < -0.5)
                    OnInput(InputMessage.ControlButton.ControlsLeft, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.Y*sign <= 0.5 && currentState.ThumbSticks.Left.Y*sign > 0.5)
                    OnInput(InputMessage.ControlButton.ControlsDown, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.Y*sign >= -0.5 && currentState.ThumbSticks.Left.Y*sign < -0.5)
                    OnInput(InputMessage.ControlButton.ControlsUp, true, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p-1].Buttons.LeftStick == ButtonState.Released && currentState.Buttons.LeftStick == ButtonState.Pressed)
                    OnInput(InputMessage.ControlButton.ControlsJump, true, null, Ballz.The().Match?.PlayerByNumber(p));

                if(previousGamePadState[p - 1].ThumbSticks.Left.X > 0.5 && currentState.ThumbSticks.Left.X <= 0.5)
                    OnInput(InputMessage.ControlButton.ControlsRight, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.X < -0.5 && currentState.ThumbSticks.Left.X >= -0.5)
                    OnInput(InputMessage.ControlButton.ControlsLeft, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.Y*sign > 0.5 && currentState.ThumbSticks.Left.Y*sign <= 0.5)
                    OnInput(InputMessage.ControlButton.ControlsDown, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p - 1].ThumbSticks.Left.Y*sign < -0.5 && currentState.ThumbSticks.Left.Y*sign >= -0.5)
                    OnInput(InputMessage.ControlButton.ControlsUp, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if(previousGamePadState[p-1].Buttons.LeftStick == ButtonState.Pressed && currentState.Buttons.LeftStick == ButtonState.Released)
                    OnInput(InputMessage.ControlButton.ControlsJump, false, null, Ballz.The().Match?.PlayerByNumber(p));
                
                if (previousGamePadState[p - 1].DPad.Up == ButtonState.Pressed && currentState.DPad.Up == ButtonState.Released)
                    OnInput(InputMessage.ControlButton.ControlsUp, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Down == ButtonState.Pressed && currentState.DPad.Down == ButtonState.Released)
                    OnInput(InputMessage.ControlButton.ControlsDown, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Left == ButtonState.Pressed && currentState.DPad.Left == ButtonState.Released)
                    OnInput(InputMessage.ControlButton.ControlsLeft, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].DPad.Right == ButtonState.Pressed && currentState.DPad.Right == ButtonState.Released)
                    OnInput(InputMessage.ControlButton.ControlsRight, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonDown(Buttons.B) && currentState.IsButtonUp(Buttons.B))
                    OnInput(InputMessage.ControlButton.ControlsBack, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonDown(Buttons.A) && currentState.IsButtonUp(Buttons.A))
                    OnInput(InputMessage.ControlButton.ControlsAction, false, null, Ballz.The().Match?.PlayerByNumber(p));
                if (previousGamePadState[p - 1].IsButtonDown(Buttons.X) && currentState.IsButtonUp(Buttons.X))
                    OnInput(InputMessage.ControlButton.ControlsJump, false, null, Ballz.The().Match?.PlayerByNumber(p));
            }

            previousGamePadState[p - 1] = currentState;
        }

        public override void Update(GameTime gameTime)
        {
            for (int i = 1; i < 5; i++)
            {
                ProcessGamePadInput(i);
            }

            currentState = Keyboard.GetState();
            
            if (currentState != previousState)
            {
                ProcessControlInput();

                previousState = currentState;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Processes the raw input and emits corresponding Events.
        /// 
        /// TODO: add GamePad Support for raw inputs.
        /// </summary>
        private void ProcessRawInput()
        {
            //the back key is supposed to switch back to processed InputMode
            //note that the RAW inputs themselves are processed by the RawHandler function.
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                OnInput(InputMessage.ControlButton.ControlsBack,true);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Back))
            {
                OnInput(InputMessage.ControlButton.RawBack);
            }
        }

        /// <summary>
        /// find out which keys got changed from array a to array b.
        /// the comparison is one sided thus if in b a key was added the returned list will be empty.
        /// if in a a key was added, the list will contain this key
        /// </summary>
        /// <returns>The keys.</returns>
        /// <param name="a">The alpha component.</param>
        /// <param name="b">The blue component.</param>
        private List<Keys> ChangedKeys(Keys[] a , Keys[] b)
        {
            List<Keys> result = new List<Keys>();
            foreach (var keyA in a)
            {
                bool keyChanged = true;
                foreach (var keyB in b)
                {
                    if (keyA == keyB)
                    {
                        keyChanged = false;
                    }
                }

                if (keyChanged)
                    result.Add(keyA);
            }

            return result;
        }

        private void ProcessControlInput()
        {
            //Keys that are pressed now but where not pressed previously are keys that got pressed
            List<Keys> pressedKeys = ChangedKeys(currentState.GetPressedKeys(), previousState.GetPressedKeys());
            //keys that where pressed previously but are not currently are Keys that got released
            List<Keys> releasedKeys = ChangedKeys(previousState.GetPressedKeys(), currentState.GetPressedKeys());

            EmitKeyMessages(pressedKeys, true);
            EmitKeyMessages(releasedKeys, false);
        }

        private void ProcessTextInput(char c)
        {
            OnInput(InputMessage.ControlButton.ControlsText, true, c, null);
        }

        private void EmitKeyMessages(List<Keys> keyList, bool pressed)
        {
            foreach(Keys theKey in keyList)
            {
                switch (theKey)
                {
                    case Keys.OemTilde:
                    case Keys.OemPipe:
                    case Keys.F1:
                        OnInput(InputMessage.ControlButton.ControlsConsole, pressed);
                        break;
                    case Keys.Escape:
                        OnInput(InputMessage.ControlButton.ControlsBack, pressed);
                        break;
                    case Keys.LeftControl:
                        OnInput(InputMessage.ControlButton.ControlsAction, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Up:
                        OnInput(InputMessage.ControlButton.ControlsUp, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Down:
                        OnInput(InputMessage.ControlButton.ControlsDown, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Left:
                        OnInput(InputMessage.ControlButton.ControlsLeft, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Right:
                        OnInput(InputMessage.ControlButton.ControlsRight, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Enter:
                    case Keys.RightControl:
                        OnInput(InputMessage.ControlButton.ControlsAction, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.Space:
                        OnInput(InputMessage.ControlButton.ControlsJump, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.PageUp:
                        OnInput(InputMessage.ControlButton.ControlsPreviousWeapon, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.PageDown:
                        OnInput(InputMessage.ControlButton.ControlsNextWeapon, pressed, null, Ballz.The().Match?.PlayerByNumber(1));
                        break;
                    case Keys.W:
                        OnInput(InputMessage.ControlButton.ControlsUp, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                    case Keys.S:
                        OnInput(InputMessage.ControlButton.ControlsDown, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                    case Keys.A:
                        OnInput(InputMessage.ControlButton.ControlsLeft, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                    case Keys.D:
                        OnInput(InputMessage.ControlButton.ControlsRight, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                    case Keys.E:
                        OnInput(InputMessage.ControlButton.ControlsAction, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                    case Keys.Q:
                        OnInput(InputMessage.ControlButton.ControlsJump, pressed, null, Ballz.The().Match?.PlayerByNumber(2));
                        break;
                }
            }
        }
    }
}