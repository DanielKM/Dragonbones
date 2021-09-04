using RTSEngine.Entities;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public class BuildingCreationTask : FactionEntityCreationTask<IBuilding> {
        [Space(), SerializeField, EnforceType(typeof(IBuilding)), Tooltip("Prefab that represents the task."), Header("Building Creation Task Properties")]
        protected GameObject prefabObject = null;
        public override GameObject PrefabObject => prefabObject;
    }
}
