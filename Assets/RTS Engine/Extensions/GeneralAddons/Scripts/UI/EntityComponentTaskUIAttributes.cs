using RTSEngine.EntityComponent;

namespace RTSEngine.UI
{
    public struct EntityComponentTaskUIAttributes : ITaskUIAttributes
    {
        public EntityComponentTaskUIData data;

        public bool launchOnce;

        public IEntityComponentGroupDisplayer sourceTracker;

        public bool locked;
        public EntityComponentLockedTaskUIData lockedData;

        public string tooltipText;
    }
}
