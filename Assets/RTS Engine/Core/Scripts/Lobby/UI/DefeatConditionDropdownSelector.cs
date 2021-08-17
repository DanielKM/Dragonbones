using System.Linq;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Lobby.UI
{
    [System.Serializable]
    public class DefeatConditionDropdownSelector : DropdownSelector<DefeatConditionType> 
    {
        [System.Serializable]
        public struct Option 
        {
            public string name;
            public DefeatConditionType condition;
        }
        [SerializeField, Tooltip("Possible choices for the defeat condition that the player can select from.")]
        private Option[] options = new Option[0];

        public DefeatConditionDropdownSelector() : base(DefeatConditionType.eliminateMain, "Defeat Condition") { }

        public void Init(ILobbyManager lobbyMgr)
        {
            elementsDic.Clear();
            foreach (Option element in options)
                elementsDic.Add(elementsDic.Count, element.condition);

            base.Init(options.Select(element => element.name), lobbyMgr);
        }
    }
}
