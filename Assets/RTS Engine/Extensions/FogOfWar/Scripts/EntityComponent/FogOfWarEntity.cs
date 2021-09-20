using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Selection;

using FoW;

namespace RTSEngine.FoW.EntityComponent
{
    public class FogOfWarEntity : FogOfWarUnit, IEntityPostInitializable, IEntityPreInitializable
    {
        private IEntity entity;

        [Header("RTS Engine"), Range(0, 255), SerializeField, Tooltip("If the fog strength/intensity is smaller than this value then the entity will be visible.")]
        private byte minFogStrength = 120;

        [SerializeField, Tooltip("Make the entity always visible to the local player if it is a free entity (does not belong to any faction).")]
        private bool visibleIfFree = true;

        // Last visibility status of the entity
        private bool lastVisible;
        // Current visibility status of the entity
        private bool isVisible;

        [SerializeField, Tooltip("Always visible post after discovery by local player? As soon as the entity is visible to the player, it will not be hidden again.")]
        private bool isVisiblePostDiscovery = false;

        [SerializeField, Tooltip("Input child objects of the entity that will be activated or deactivated depending on the entity's visibility status.")]
        private GameObject[] sameVisibilityObjects = new GameObject[0];

        private bool isInitiated = false;

        // Services
        protected IGameManager gameMgr { private set; get; } 
        protected IFogOfWarRTSManager fowMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            // Have the component disabled before the entity is completely initialized
            enabled = false;
        }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            enabled = true;

            this.entity = entity;

            this.gameMgr = gameMgr;
            this.fowMgr = gameMgr.GetService<IFogOfWarRTSManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            UpdateFoWTeam();

            entity.FactionUpdateComplete += HandleFactionUpdateComplete;
        }

        private void UpdateFoWTeam()
        {
            team = entity.IsFree 
                ? (visibleIfFree ? gameMgr.LocalFactionSlotID : -1)
                : entity.FactionID;
        }

        public void Disable()
        {
            enabled = false;
        }

        private void HandleFactionUpdateComplete(IEntity sender, FactionUpdateArgs args)
        {
            UpdateFoWTeam();
            // Updating the faction disables the entity components which disables this component so we have to re-enable this component here
            enabled = true;
        }

        private void Update()
        {
            // If the entity is supposed to remain visible after it is discovered by the local player, then do not change the visiblity anymore
            if (isInitiated && isVisible && isVisiblePostDiscovery)
                return;

            isVisible = !fowMgr.IsInFog(entity.transform.position, minFogStrength);

            // Same visibility state? Nothing to update
            // If this is the first time for which component checks for visiblity, allow it even if it does not change the state
            // The reason for the latter point is that we want to fire the visibility event below so that other components can change their state depending on the visiblity of this entity
            if (isInitiated && isVisible == lastVisible)
                return;

            isInitiated = true;

            // New visibility state, update:
            lastVisible = isVisible;

            globalEvent.RaiseEntityVisibilityUpdateGlobal(entity, new VisibilityEventArgs(isVisible));

            // The entity is now hidden
            if(!isVisible)
            {
                if (entity.Selection.IsSelected)
                    selectionMgr.Remove(entity);
            }

            foreach (GameObject obj in sameVisibilityObjects)
                obj.SetActive(isVisible);

            // HANDLING MINIMAP ICONS?
        }
    }
}
