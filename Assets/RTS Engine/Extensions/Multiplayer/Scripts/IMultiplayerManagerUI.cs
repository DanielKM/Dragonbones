using RTSEngine.UI.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Multiplayer
{
    public interface IMultiplayerManagerUI : IMonoBehaviour
    {
        ITextMessage Message { get; }

        void Init(IMultiplayerManager multiplayerMgr);
    }
}
