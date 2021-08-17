using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine.Faction
{
    [CreateAssetMenu(fileName = "NewFactionType", menuName = "RTS Engine/Faction Type", order = 1)]
    public class FactionTypeInfo : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("The name of the faction type to display in UI elements.")]
        private string _name = "Faction0";
        public string Name => _name;

        [SerializeField, Tooltip("Unique code for the faction type.")]
        private string code = "faction0";
        /// <summary>
        /// Gets the unique code of the faction type.
        /// </summary>
        public override string Key => code;

        [SerializeField, Tooltip("Define amount limits for units and buildings for this faction type.")]
        private List<FactionEntityAmountLimit> limits = new List<FactionEntityAmountLimit>(); 
        public IEnumerable<FactionEntityAmountLimit> Limits => limits;
    }
}
