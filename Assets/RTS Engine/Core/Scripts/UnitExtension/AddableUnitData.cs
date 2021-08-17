using RTSEngine.EntityComponent;

namespace RTSEngine.UnitExtension
{
    [System.Serializable]
    public struct AddableUnitData
    {
        public bool allowDifferentFaction;

        public IEntityTargetComponent sourceComponent;

        public bool playerCommand;
    }
}
