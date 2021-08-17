using System.Collections.Generic;
using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Effect;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Audio
{
    public class GameAudioManager : AudioManagerBase, IGameAudioManager
    {
        #region Attributes
        private List<AudioSource> localAudioSources = new List<AudioSource>(); //holds all local audio source instances in the game (coming from units, buildings, resources and custom events).

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            InitBase(logger);

            //subscribe to following events to monitor creation and destruction of entities:
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            globalEvent.EffectObjectCreatedGlobal += HandleEffectObjectCreatedGlobal;
            globalEvent.EffectObjectDestroyedGlobal += HandleEffectObjectDestroyedGlobal;
        }

        protected override void OnDisabled()
        {
            //unsub to following events:
            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal -= HandleEntityDeadGlobal;

            globalEvent.EffectObjectCreatedGlobal -= HandleEffectObjectCreatedGlobal;
            globalEvent.EffectObjectDestroyedGlobal -= HandleEffectObjectDestroyedGlobal;
        }
        #endregion

        #region Handling Events: IEffectObject
        private void HandleEffectObjectCreatedGlobal(IEffectObject effectObject, EventArgs e)
        {
            AddLocalAudioSource(effectObject.AudioSourceComponent);
        }

        private void HandleEffectObjectDestroyedGlobal(IEffectObject effectObject, EventArgs e)
        {
            localAudioSources.Remove(effectObject.AudioSourceComponent);
        }
        #endregion

        #region Handling Events: IEntity
        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            AddLocalAudioSource(entity.AudioSourceComponent); //add the entity's audio source component to the list.
        }

        private void HandleEntityDeadGlobal(IEntity entity, DeadEventArgs e)
        {
            localAudioSources.Remove(entity.AudioSourceComponent); //remove the entity's audio source component from the list.
        }
        #endregion

        #region Local Audio Sources
        private void AddLocalAudioSource(AudioSource newSource)
        {
            if (newSource == null)
                return;

            newSource.volume = Data.SFXVolume;
            localAudioSources.Add(newSource);
        }

        protected override void OnAudioDataUpdated()
        {
            foreach (AudioSource source in localAudioSources)
                source.volume = Data.SFXVolume;
        }
        #endregion
    }
}