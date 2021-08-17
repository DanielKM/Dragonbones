using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Lobby.Logging;

namespace RTSEngine.Lobby.UI
{
    [System.Serializable]
    public abstract class DropdownSelector<T>
    {
        protected Dictionary<int, T> elementsDic = new Dictionary<int, T>();
        public IEnumerable<string> OptionNames { private set; get; }

        // Serivces
        private ILobbyLoggingService logger;

        [SerializeField]
        private Dropdown menu = null;

        public bool Interactable
        {
            set
            {
                menu.interactable = value;
            }

            get
            {
                return menu.interactable;
            }
        }

        // The name of the value type that this drop down menu is handling
        private readonly string name;

        // The default value of the type to return if the drop down menu value isn't valid.
        public int CurrentOptionID => menu.value;
        private readonly T defaultValue;
        public T CurrentValue => GetValue(menu.value);
        public T GetValue(int index) => elementsDic.TryGetValue(index, out T returnValue) ? returnValue : defaultValue;

        public DropdownSelector(T defaultValue, string name)
        {
            this.defaultValue = defaultValue;
            this.name = name;
        }

        protected void Init(IEnumerable<string> optionNames, ILobbyManager lobbyMgr)
        {
            this.OptionNames = optionNames;
            this.logger = lobbyMgr.GetService<ILobbyLoggingService>();

            if (!logger.RequireValid(menu, $"[{GetType().Name}] The drop down menu of the '{name}' hasn't been assigned."))
                return;

            menu.ClearOptions();
            menu.AddOptions(optionNames.ToList()); 
        }

        public void SetOption (int optionID)
        {
            menu.value = optionID;
        }
    }
}
