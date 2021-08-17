using System.Linq;

using UnityEngine;

namespace RTSEngine.Lobby.UI
{
    [System.Serializable]
    public class TimeModifierDropdownSelector : DropdownSelector<float> 
    {
        [System.Serializable]
        public struct Option 
        {
            public string name;
            public float modifier;
        }
        [SerializeField, Tooltip("Possible choices for the time modifier that the player can select from.")]
        private Option[] options = new Option[0];

        public TimeModifierDropdownSelector() : base(1.0f, "Time Modifier") { }

        public void Init(ILobbyManager lobbyMgr)
        {
            elementsDic.Clear();
            foreach (Option element in options)
                elementsDic.Add(elementsDic.Count, element.modifier);

            base.Init(options.Select(element => element.name), lobbyMgr);
        }
    }
}
