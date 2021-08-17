using RTSEngine.Game;

namespace RTSEngine.Selection
{
    public interface IIdleUnitSelector : IPreRunGameService
    {
        void SelectIdleUnits();
    }
}