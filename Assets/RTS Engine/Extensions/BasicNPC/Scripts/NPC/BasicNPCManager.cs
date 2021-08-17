using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.NPC
{
    public class BasicNPCManager : MonoBehaviour, INPCManager
    {
        #region Attributes
        public NPCType Type { private set; get; }

        private Dictionary<Type, INPCComponent> oneInstanceComponents;
        private Dictionary<Type, IEnumerable<INPCComponent>> multipleInstanceComponents;

        public IFactionManager FactionMgr { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<INPCManager, EventArgs> InitComplete;
        private void RaiseInitComplete()
        {
            var handler = InitComplete;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(NPCType npcType, IGameManager gameMgr, IFactionManager factionMgr)
        {
            this.Type = npcType;
            this.gameMgr = gameMgr;
            this.FactionMgr = factionMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            gameMgr.GameStartRunning += HandleGameStartRunning;

            oneInstanceComponents = new Dictionary<Type, INPCComponent>();
            multipleInstanceComponents = new Dictionary<Type, IEnumerable<INPCComponent>>();
        }

        private void OnDestroy()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }

        private void HandleGameStartRunning(IGameManager sender, EventArgs args)
        {
            var allComponents = GetComponentsInChildren<INPCComponent>();
            var componentGroups = allComponents 
                .GroupBy(component => component.IsSingleInstance);

            oneInstanceComponents = componentGroups
                // Fetch singular components
                .Where(group => group.Key)
                .SelectMany(group => group)
                .ToDictionary(
                component => component.GetType().GetSuperInterfaceType<INPCComponent>(),
                component => component
                );

            multipleInstanceComponents = componentGroups
                // Fetch sets of components
                .Where(group => !group.Key)
                .SelectMany(group => group)
                .GroupBy(component => component.GetType().GetSuperInterfaceType<INPCComponent>())
                .ToDictionary(
                group => group.Key,
                group => group.Select(component => component));

            foreach (var component in allComponents)
                component.Init(gameMgr, this);

            RaiseInitComplete();
        }
        #endregion

        #region NPC Component Handling
        public T GetNPCComponent<T>() where T : INPCComponent
        {
            if (!logger.RequireTrue(oneInstanceComponents.ContainsKey(typeof(T)),
                $"[NPCManager - {FactionMgr.FactionID}] NPC Faction does not have an active instance of type '{typeof(T)}' that implements the '{typeof(INPCComponent).Name}' interface!"))
                return default;

            return (T)oneInstanceComponents[typeof(T)];
        }

        public IEnumerable<T> GetNPCComponentSet<T>() where T : INPCComponent
        {
            if (!logger.RequireTrue(multipleInstanceComponents.ContainsKey(typeof(T)),
                $"[NPCManager - Faction ID: {FactionMgr.FactionID}] NPC Faction does not have an active set of instances of type '{typeof(T)}' that implement the '{typeof(INPCComponent).Name}' interface!"))
                return default;

            return multipleInstanceComponents[typeof(T)].ToArray().Cast<T>();
        }
        #endregion
    }
}
