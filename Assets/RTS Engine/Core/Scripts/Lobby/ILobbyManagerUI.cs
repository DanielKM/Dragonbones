using RTSEngine.Lobby.Service;
using RTSEngine.UI.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Lobby
{
    public interface ILobbyManagerUI : IMonoBehaviour, ILobbyService
    {
        void SetInteractable(bool interactable);
        void Toggle(bool show);
    }
}
