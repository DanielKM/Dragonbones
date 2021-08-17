using RTSEngine.Upgrades;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public class UpgradeTask : EntityComponentTaskInputBase<Upgrade> {

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
