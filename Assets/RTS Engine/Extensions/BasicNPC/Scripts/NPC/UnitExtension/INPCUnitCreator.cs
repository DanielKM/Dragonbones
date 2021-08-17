using RTSEngine.Entities;
using RTSEngine.ResourceExtension;

namespace RTSEngine.NPC.UnitExtension
{
    public interface INPCUnitCreator : INPCComponent
    {
        ResourceTypeInfo PopulationResource { get; }

        NPCUnitRegulator ActivateUnitRegulator(IUnit unitPrefab);

        NPCUnitRegulator GetActiveUnitRegulator(string unitCode);

        bool OnCreateUnitRequest(string unitCode, int requestedAmount, out int createdAmount);
    }
}