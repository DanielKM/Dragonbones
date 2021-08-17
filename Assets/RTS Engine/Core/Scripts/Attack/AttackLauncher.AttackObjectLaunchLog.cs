using RTSEngine.Determinism;

namespace RTSEngine.Attack
{
    public struct AttackObjectLaunchLog
    {
        public int sourceIndex;

        public TimeModifiedTimer preDelayTimer;
        public TimeModifiedTimer postDelayTimer;

        public IAttackObject attackObject;

        // When 'isLastLaunch' is true, it means that this is the last attack object launch in the attack iteration, that will call the 'OnComplete' method on the IAttackComponent
        public bool isLastLaunch;

        public AttackObjectLaunchLog(AttackObjectSource[] sources, int sourceIndex, bool isLastLaunch)
        {
            this.sourceIndex = sourceIndex;

            preDelayTimer = new TimeModifiedTimer(sources[sourceIndex].preDelayTime);
            postDelayTimer = new TimeModifiedTimer(sources[sourceIndex].postDelayTime);

            attackObject = null;

            this.isLastLaunch = isLastLaunch;
        }
    }
}
