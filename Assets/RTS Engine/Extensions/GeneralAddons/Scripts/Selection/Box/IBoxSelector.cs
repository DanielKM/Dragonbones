using RTSEngine.Game;

namespace RTSEngine.Selection.Box
{
    public interface IBoxSelector : IPreRunGameService
    {
        bool IsActive { get; }

        void Disable();
    }
}