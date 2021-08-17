namespace RTSEngine.NPC.UnitExtension
{
    public interface INPCUnitBehaviourManager : INPCComponent
    {
        NPCUnitBehaviourState State { get; }
    }
}