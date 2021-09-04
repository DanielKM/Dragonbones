using RTSEngine.Entities;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public class SpellCastTask : FactionEntityCreationTask<ISpell> {
        [Space(), SerializeField, EnforceType(typeof(ISpell)), Tooltip("Prefab that represents the task."), Header("Spell Creation Task Properties")]
        protected GameObject prefabObject = null;
        public override GameObject PrefabObject => prefabObject;
    }
}
