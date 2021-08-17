using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.UI;
using RTSEngine.Event;

namespace RTSEngine.Logging
{
    public struct PlayerErrorMessageWrapper
    {
        public ErrorMessage message;

        // entities involved in the error message
        public IEntity source;
        public IEntity target;
    }

    public interface IPlayerMessageHandler : IPreRunGameService
    {
        void OnErrorMessage(PlayerErrorMessageWrapper msgWrapper);
    }

    // This class handles interpreting messages that should be communicated to the player.
    public class PlayerMessageHandler : MonoBehaviour, IPlayerMessageHandler 
    {
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameUITextDisplayManager gameTextDisplayUI { private set; get; } 

        public static IEnumerable<ErrorMessage> SuccessErrorMessages => new ErrorMessage[] { ErrorMessage.none };

        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.gameTextDisplayUI = gameMgr.GetService<IGameUITextDisplayManager>(); 
        }

        public void OnErrorMessage (PlayerErrorMessageWrapper msgWrapper)
        {
            switch(msgWrapper.message)
            {
                case ErrorMessage.none:
                case ErrorMessage.invalid:
                case ErrorMessage.inactive:
                    return;

                default:
                    if (gameTextDisplayUI.PlayerErrorMessageToString(msgWrapper, out string displayText))
                        globalEvent.RaiseShowPlayerMessageGlobal(this, new MessageEventArgs
                        (
                            type: MessageType.error,
                            message: displayText
                        ));
                    break;
            }
        }
    }
}
