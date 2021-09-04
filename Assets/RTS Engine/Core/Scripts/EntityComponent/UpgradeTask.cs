using RTSEngine.Upgrades;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public class UpgradeTask : EntityComponentTaskInputBase<Upgrade> {

        [Space(), SerializeField, EnforceType(typeof(Upgrade)), Tooltip("Prefab that represents the task."), Header("Upgrade Task Properties")]
        protected GameObject prefabObject = null;
        public override GameObject PrefabObject => prefabObject;

        private bool locked = false;

        public override ErrorMessage CanStart()
        {
            if (locked)
                return ErrorMessage.locked;

            return base.CanStart();
        }

        public override void OnStart()
        {
            base.OnStart();

            locked = true;
        }

        public override void OnCancel()
        {
            base.OnCancel();

            locked = false;
        }
    }
}
