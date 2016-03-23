using System;
using System.Collections.Generic;
using System.Linq;
using Ballz.Input;
using Ballz.Menu;
using Ballz.Messages;

namespace Ballz.Logic
{
    /// <summary>
    ///     Logic control Processes Messages and other system reactions with regard to the current gamestate.
    ///     It uses Message events to inform relevant classes.
    /// </summary>
    public class LogicControl
    {
        private readonly Stack<Composite> activeMenu = new Stack<Composite>();
        private GameState state;
        private bool rawInput;
        private Ballz Game;

        public LogicControl(Ballz game)
        {
            Game = game;

            Composite menu = Game.MainMenu;// = DefaultMenu();
            //push the root menuToPrepare
            activeMenu.Push(menu); //TODO: uncast
            RegisterMenuEvents(menu);

            state = GameState.MenuState;
        }

        public void StartGame(GameSession.Logic.GameSettings settings)
        {
            // Go back to main menu so it will show when the user enters the menu later
            MenuGoBack();
            // Select the "Continue" entry
            activeMenu.Peek().SelectIndex(0);

            state = GameState.SimulationState;
            if (Game.Match != null)
                Game.Match.Dispose();

            Game.Match = settings.GameMode.StartSession(Game, settings);
            Game.Match.Start();
            RaiseMessageEvent(new LogicMessage(LogicMessage.MessageType.GameMessage));
        }

        public void ContinueGame()
        {
            state = GameState.SimulationState;
            RaiseMessageEvent(new LogicMessage(LogicMessage.MessageType.GameMessage));
        }

        private void RegisterMenuEvents(Item menu)
        {
            menu.BindSelectHandler<Composite>(c =>
            {
                activeMenu.Push(c);
                RaiseMessageEvent(new MenuMessage(activeMenu.Peek()));
            });

            menu.BindSelectHandler<Back>(b =>
            {
                activeMenu.Pop();
                RaiseMessageEvent(new MenuMessage(activeMenu.Peek()));
            });

            menu.BindSelectHandler<InputBox>(ib =>
            {
                rawInput = true;
                RaiseMessageEvent(new MenuMessage(ib));
            });

            menu.BindUnSelectHandler<Item>(i =>
            {
                if (!i.Selectable || !i.Active && !i.ActiveChanged)
                {
                    activeMenu.Pop();
                    RaiseMessageEvent(new MenuMessage(activeMenu.Peek()));
                }
            });
        }

        public event EventHandler<Message> Message;

        protected virtual void RaiseMessageEvent(Message msg)
        {
            Message?.Invoke(this, msg);
        }

        public void HandleNetworkMessage(object sender, Message message)
        {
            if (message.Kind != Messages.Message.MessageType.NetworkMessage)
                return;
            var msg = (NetworkMessage)message;
            switch (msg.Kind)
            {
                case NetworkMessage.MessageType.ConnectedToServer:
                    //TODO: show lobby!!!!<<<<<<<<<<<<<<<<<<<<<<<<<
                    //Game is started by a message from server
                    break;
            }
        }

        public void HandleInputMessage(object sender, Message message)
        {
            if (message.Kind != Messages.Message.MessageType.InputMessage)
                return;

            if (((InputMessage)message).Control == InputMessage.ControlButton.ControlsConsole && ((InputMessage)message).Pressed.Value)
                RaiseMessageEvent(new LogicMessage(LogicMessage.MessageType.PerformanceMessage));

            switch (state)
            {
                case GameState.MenuState:
                    MenuLogic((InputMessage)message);
                    break;
                case GameState.SimulationState:
                    GameLogic((InputMessage)message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CheckInputMode((InputTranslator)sender);
        }

        private void GameLogic(InputMessage msg)
        {
            if (msg.Pressed.Value)
            {
                switch (msg.Control)
                {
                    case InputMessage.ControlButton.ControlsBack:
                        state = GameState.MenuState;
                        RaiseMessageEvent(new LogicMessage(LogicMessage.MessageType.GameMessage));
                        //todo: implement LogicMessage and use it here
                        break;
                    case InputMessage.ControlButton.ControlsUp:
                        break;
                    case InputMessage.ControlButton.ControlsDown:
                        break;
                    case InputMessage.ControlButton.ControlsLeft:
                        break;
                    case InputMessage.ControlButton.ControlsRight:
                        break;
                    case InputMessage.ControlButton.ControlsAction:
                        break;
                    case InputMessage.ControlButton.RawInput:
                        break;
                    default:
                        //throw new ArgumentOutOfRangeException();
                        break;
                }
            }
        }

        private void MenuLogic(InputMessage msg)
        {
            Composite top = activeMenu.Peek();
            if (msg.Control == InputMessage.ControlButton.RawInput || msg.Control == InputMessage.ControlButton.RawBack || msg.Pressed.Value)
            {
                switch (msg.Control)
                {
                    case InputMessage.ControlButton.ControlsAction:
                        top.SelectedItem?.Activate();
                        break;
                    case InputMessage.ControlButton.ControlsBack:
                        if (activeMenu.Count == 1) // exit if we are in main menuToPrepare
                            Ballz.The().Exit();     //TODO: this is rather ugly find a nice way to terminate the programm like sending a termination message
                        else
                        {
                            if (rawInput)
                                rawInput = false;
                            else
                            {
                                MenuGoBack();
                            }
                        }

                        RaiseMessageEvent(new MenuMessage(activeMenu.Peek()));
                        break;
                    case InputMessage.ControlButton.ControlsUp:
                        if (top.SelectedItem != null)
                        {
                            top.SelectPrevious();
                            RaiseMessageEvent(new MenuMessage(top));
                        }

                        break;
                    case InputMessage.ControlButton.ControlsDown:
                        if (top.SelectedItem != null)
                        {
                            top.SelectNext();
                            RaiseMessageEvent(new MenuMessage(top));
                        }

                        break;
                    case InputMessage.ControlButton.ControlsLeft:
                        (top.SelectedItem as IChooseable)?.SelectPrevious();
                        break;
                    case InputMessage.ControlButton.ControlsRight:
                        (top.SelectedItem as IChooseable)?.SelectNext();
                        break;
                    case InputMessage.ControlButton.RawInput:
                        if (msg.Key != null)
                            (top.SelectedItem as IRawInputConsumer)?.HandleRawKey(msg.Key.Value);
                        break;
                    case InputMessage.ControlButton.RawBack:
                        (top.SelectedItem as IRawInputConsumer)?.HandleBackspace();
                        break;
                    default:
                        //throw new ArgumentOutOfRangeException();
                        break;
                }
            }
        }

        private void MenuGoBack()
        {
            var top = activeMenu.Peek();
            if (top.SelectedItem != null)
                top.SelectedItem.DeActivate();
            else
                top.DeActivate();
        }

        /// <summary>
        /// Checks the input mode.
        /// TODO: refactor the Menu logic to a menuLogic class or use a partial class definition as this file seems to become messy
        /// </summary>
        void CheckInputMode(InputTranslator translator)
        {
        }

        private enum GameState
        {
            MenuState,
            SimulationState
        }
    }
}