using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using RTSEngine.Entities;

namespace RTSEngine.NPC
{
    public class NPCRegulatorData : ScriptableObject
    {
        [SerializeField, Tooltip("Minimum amount of instances to create.")]
        private IntRange minAmountRange = new IntRange(1, 2);
        public int MinAmount => minAmountRange.RandomValue;

        [SerializeField, Tooltip("Maximum amount of instances to be created.")]
        private IntRange maxAmountRange = new IntRange(10, 15);
        public int MaxAmount => maxAmountRange.RandomValue;

        [SerializeField, Tooltip("Maximum amount of instances that can be pending creation at the same time.")]
        private IntRange maxPendingAmount = new IntRange(1,2);
        public int MaxPendingAmount => maxPendingAmount.RandomValue;

        [SerializeField, Tooltip("Can NPC Components (except the NPCUnitCreator) request to create this?")]
        private bool createOnDemand = true;
        public bool CanCreateOnDemand => createOnDemand;

        [SerializeField, Tooltip("When should the NPC start creating the first instance after the game starts?")]
        private FloatRange startCreatingAfter = new FloatRange(10.0f, 15.0f);
        public float CreationDelayTime => startCreatingAfter.RandomValue;

        [SerializeField, Tooltip("Time required between spawning two consecutives instances.")]
        private FloatRange spawnReloadRange = new FloatRange(15.0f, 20.0f); 
        public float SpawnReload => spawnReloadRange.RandomValue;

        [SerializeField, Tooltip("Automatically create instances when requirements are met? When enabled, the NPC components will look to enforce the specified minimum amount")]
        private bool autoCreate = true;
        public bool CanAutoCreate => autoCreate;

        [SerializeField, Tooltip("Input the faction units/buildings required for the NPC faction to have created before it can create this faction entity type.")]
        protected FactionEntityRequirement[] factionEntityRequirements = new FactionEntityRequirement[0];
        public IEnumerable<FactionEntityRequirement> FactionEntityRequirements => factionEntityRequirements.ToList();
    }
}
