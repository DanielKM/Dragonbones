using System.Collections;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Search;
using RTSEngine.Determinism;
using System;
using System.Linq;

namespace RTSEngine.EntityComponent
{
    /// <summary>
    /// Allows to define a search process (for an Entity instance).
    /// </summary>
    [System.Serializable]
    public class TargetEntityFinder<T> where T : IEntity
    {
        [SerializeField, Tooltip("Enable finding targets and set search period time.")]
        private GlobalTimeModifiedTimer reload = new GlobalTimeModifiedTimer(enabled: true, defaultValue: 1.0f);

        public IEntityTargetComponent Source { get; private set; }

        private bool enabled = false;
        /// <summary>
        /// Set or get whether searching for a target entity is active or not.
        /// </summary>
        public bool Enabled { 
            set 
            {
                enabled = value;
                reload.IsActive = enabled;
            }
            get => enabled;
        }

        // Where the search will be conducted from.
        private Transform center;
        public Transform Center
        {
            set
            {
                if (value.IsValid())
                    center = value;
            }
            get => center.IsValid() ? center : Source.Entity.transform;
        }

        private float range;
        /// <summary>
        /// 
        /// </summary>
        public float Range
        {
            set
            {
                if (value > 0.0f)
                    range = value;
            }
            get => range;
        }

        public bool IdleOnly { set; get; }

        public TargetEntityFinderData Data => new TargetEntityFinderData
        {
            enabled = Enabled,
            idleOnly = IdleOnly,
            range = range,
            reloadTime = reload.DefaultValue
        };

        protected IGridSearchHandler gridSearch { private set; get; }

        /// <summary>
        /// Initializes a TargetEntityFinder instance.
        /// </summary>
        /// <param name="source">Entity instance that the TargetEntityFinder is finding a target for.</param>
        public TargetEntityFinder (IGameManager gameMgr, IEntityTargetComponent source, Transform center, TargetEntityFinderData data)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            this.Source = source;
            this.Center = center;

            //apply for the initial parameters from the provided data:
            reload.Init(gameMgr, SearchTarget);
            Range = data.range;
            IdleOnly = data.idleOnly;

            Enabled = data.enabled;
        }

        public void SearchTarget()
        {
            if (RTSHelper.IsMasterInstance()
                && !Source.HasTarget
                && (!IdleOnly || Source.Entity.IsIdle)
                && Source.CanSearch
                && gridSearch.Search(Center.position, Range, Source.IsTargetValid, playerCommand: false, out T potentialTarget) == ErrorMessage.none)
            {
                Source.SetTarget(new TargetData<IEntity> { instance = potentialTarget, position = potentialTarget.transform.position }, playerCommand: false);
            }

            // Reload timer
            if(Enabled)
                reload.IsActive = true;
        }
    }
}
