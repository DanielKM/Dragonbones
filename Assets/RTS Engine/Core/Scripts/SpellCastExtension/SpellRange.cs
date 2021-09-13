using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;

namespace RTSEngine.SpellCastExtension
{
    public class SpellRange : MonoBehaviour, ISpellRange
    {
        #region Attributes
        public ISpell Spell { private set; get; }

        public bool IsActive { private set; get; }

        [SerializeField, Tooltip("Size of the territory that this component adds to the faction.")]
        private float size = 10.0f;
        public float Size { private set => size = value; get { return size; } }
        public float Surface => Mathf.PI * Mathf.Pow(Size, 2);

        [SerializeField, EnforceType(typeof(ISpellRangeObject), prefabOnly: true), Tooltip("Assign a Border Object prefab to this field to spawn it.")]
        private GameObject borderPrefab = null;

        //the sorting order of this border, if border A has been activated before border B then border A has higher order than border B.
        //the order is used to determine which has priority over a common area of the map
        public int SortingOrder { private set; get; }

        [Space(), SerializeField, Tooltip("Set the maximum amount of instances of spell types that can be inside this border.")]
        private SpellAmount[] spellLimits = new SpellAmount[0];

        //key: code of a spell type that is inside this border's territory
        //value: amount of the spell instances inside this border's territory of the spell type in key.
        private Dictionary<string, int> spellTypeTracker = new Dictionary<string, int>();

        //a list of the spawned spells inside the territory defined by this border
        private List<ISpell> spellsInRange = new List<ISpell>(); 
        public IEnumerable<ISpell> SpellsInRange => spellsInRange.ToList();

        //a list of the resources inside the territory defined by this border
        private List<IResource> resourcesInRange = new List<IResource>(); 
        public IEnumerable<IResource> ResourcesInRange => resourcesInRange.ToList();

        public IEnumerable<IEntity> EntitiesInRange => SpellsInRange.Cast<IEntity>().Concat(resourcesInRange);

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; } 
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, ISpell spell)
        {
            if (IsActive)
                return;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>(); 

            this.Spell = spell;

            spellsInRange.Add(Spell);

            SortingOrder = gameMgr.GetService<ISpellCastManager>().LastBorderSortingOrder;

            if(borderPrefab.IsValid())
            {
                borderPrefab = Instantiate(borderPrefab, transform.position, Quaternion.identity);
                borderPrefab.GetComponent<ISpellRangeObject>().Init(gameMgr, this);
            }

            IsActive = true;

            // globalEvent.ResourceInitiatedGlobal += HandleResourceInitiatedGlobal;
            // globalEvent.ResourceDeadGlobal += HandleResourceDeadGlobal;

            // globalEvent.BorderResourceRemovedGlobal += HandleBorderResourceRemovedGlobal;

            // globalEvent.RaiseBorderActivatedGlobal(this);

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable ()
        {
            if (!IsActive)
                return;  

            // RemoveAllResources();

            // globalEvent.ResourceInitiatedGlobal -= HandleResourceInitiatedGlobal;
            // globalEvent.ResourceDeadGlobal -= HandleResourceDeadGlobal;

            // globalEvent.BorderResourceRemovedGlobal -= HandleBorderResourceRemovedGlobal;

            // globalEvent.RaiseBorderDisabledGlobal(this);

            if (borderPrefab.IsValid())
                Destroy(borderPrefab.gameObject);

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Territory Handling
        public bool IsInRange (Vector3 testPosition)
        {
            return Vector3.Distance(testPosition, transform.position) <= Size;
        }
        #endregion

        // #region Handling Events
        // private void HandleResourceInitiatedGlobal (IResource resource, EventArgs e)
        // {
        //     AddResource(resource);
        // }

        // private void HandleResourceDeadGlobal (IResource resource, DeadEventArgs e)
        // {
        //     RemoveResource(resource);
        // }

        // private void HandleBorderResourceRemovedGlobal(object sender, ResourceEventArgs e)
        // {
        //     if (sender as SpellRange == this) //make sure this is not the same border that removed the resource
        //         return;

        //     AddResource(e.Resource);
        // }
        // #endregion

        #region Border Spell Manipulation
        public void RegisterSpell(ISpell spell)
        {
            spellsInRange.Add(spell);

            if (!spellTypeTracker.ContainsKey(spell.Code))
                spellTypeTracker.Add(spell.Code, 0);

            spellTypeTracker[spell.Code] += 1;

            OnSpellRegistererd(spell);
        }

        protected virtual void OnSpellRegistererd(ISpell spell) { }

        public void UnegisterSpell(ISpell spell)
        {
            spellsInRange.Remove(spell);

            spellTypeTracker[spell.Code] -= 1;

            OnSpellUnregistererd(spell);
        }

        protected virtual void OnSpellUnregistererd(ISpell spell) { }

        public virtual bool AllowSpellInBorder(ISpell spell)
        {
            foreach(SpellAmount ba in spellLimits)
                if(ba.codes.Contains(spell))
                {
                    spellTypeTracker.TryGetValue(spell.Code, out int currValue);

                    return currValue < ba.amount;
                }

            return true; //if the spell type doesn't have a defined slot in the spells limits, then it can be definitely accepted.
        }
        #endregion
    }
}