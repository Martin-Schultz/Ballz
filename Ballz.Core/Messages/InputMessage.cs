using Ballz.GameSession.Logic;
using System;

namespace Ballz.Messages
{
    [Serializable]
    public class InputMessage : Message
    {
        public new enum ControlButton
        {
            ControlsText,
            ControlsConsole,
            ControlsUp,
            ControlsDown,
            ControlsLeft,
            ControlsRight,
            ControlsAction,
            ControlsJump,
            ControlsBack,
            ControlsNextWeapon,
            ControlsPreviousWeapon,
            RawBack,
            RawInput
        }

        public char? Key{ get; private set;}

        public bool? Pressed{ get; private set;}

        public Player Player { get; private set; }

        public InputMessage(ControlButton key, bool? pressed, char? value, Player player) : base(Message.MessageType.InputMessage)
        {
            Control = key;
            Key = value;
            Pressed = pressed;
            Player = player;
        }

        public new ControlButton Control { get; private set; }
    }
}