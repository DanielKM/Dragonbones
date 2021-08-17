using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Logging;
using RTSEngine.Game;
using RTSEngine.Minimap.Cameras;

namespace RTSEngine.Minimap.Notification
{
    public class AttackMinimapNotificationManager : MonoBehaviour, IAttackMinimapNotificationManager
    {
        #region Attributes
        [SerializeField, EnforceType(sameScene: true), Tooltip("UI canvas used to render minimap UI elements.")]
        private Canvas minimapCanvas = null;

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Attack warning prefab as an effect object.")]
        private GameObject effectPrefab = null;
        private IEffectObject effect = null;

        [SerializeField, Tooltip("Played when a new attack warning is spawned.")]
        private AudioClipFetcher audioClip = new AudioClipFetcher();
        [SerializeField, Tooltip("When enabled, a player message is sent to the IPlayerMessageHandler manager component which interpretes the message and can communicate it to the player.")]
        private bool sendPlayerMessage = true;

        [SerializeField, Tooltip("The minimum distance required between all active attack warnings.")]
        private float minDistance = 10.0f;

        protected IGameLoggingService logger { private set; get; }
        protected IMinimapCameraController minimapCameraController { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.minimapCameraController = gameMgr.GetService<IMinimapCameraController>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();

            if(effectPrefab)
                effect = effectPrefab.GetComponent<IEffectObject>();

            if (!logger.RequireValid(minimapCanvas,
                $"[{GetType().Name}] The 'Minimap Canvas' field hasn't been assigned!",
                source: this)
                || !logger.RequireValid(effect,
                $"[{GetType().Name}] The 'Prefab' field hasn't been assigned!",
                source: this))
                return;

            globalEvent.FactionEntityHealthUpdatedGlobal += HandleFactionEntityHealthUpdatedGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.FactionEntityHealthUpdatedGlobal += HandleFactionEntityHealthUpdatedGlobal;
        }
        #endregion

        #region Handling Event: Faction Entity Health Updated
        private void HandleFactionEntityHealthUpdatedGlobal(IFactionEntity factionEntity, HealthUpdateEventArgs args)
        {
            // Local player's faction entity received damage?
            if (RTSHelper.IsLocalPlayerFaction(factionEntity)
                && args.Value < 0)
                Spawn(factionEntity.transform.position);
        }
        #endregion

        #region Spawning Minimap Attack Notifications
        /// <summary>
        /// Checks whether a new attack warning effect can be added in a potential position.
        /// </summary>
        public bool CanSpawn(Vector3 spawnPosition)
        {
            return effectObjPool.ActiveEffectObjects.TryGetValue(effect.Code, out IEnumerable<IEffectObject> currActiveSet)
                ? currActiveSet
                    .All(activeEffect => Vector3.Distance(activeEffect.GetComponent<RectTransform>().localPosition, spawnPosition) > minDistance)
                : true;
        }

        /// <summary>
        /// Spawns a new attack warning.
        /// </summary>
        public void Spawn(Vector3 targetPosition)
        {
            if (!logger.RequireTrue(RectTransformUtility.ScreenPointToLocalPointInRectangle(
                minimapCanvas.GetComponent<RectTransform>(),
                minimapCameraController.WorldToScreenPoint(targetPosition),
                minimapCameraController.MinimapCamera,
                out Vector2 canvasPosition),
                $"[{GetType().Name}] Unable to find the target position for the new attack warning effect on the minimap canvas!"))
                return;

            Vector3 spawnPosition = new Vector3(canvasPosition.x, canvasPosition.y, 0.0f);

            if (!CanSpawn(spawnPosition))
                return;

            effectObjPool.Spawn(effectPrefab, spawnPosition, effectPrefab.GetComponent<RectTransform>().localRotation,
               minimapCanvas.transform, enableLifeTime: true, autoLifeTime: true, lifeTime: 0.0f, isUIElement: true);

            audioMgr.PlaySFX(audioClip.Fetch(), false);

            if (sendPlayerMessage)
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = ErrorMessage.factionUnderAttack,
                });
        }
    }
    #endregion
}