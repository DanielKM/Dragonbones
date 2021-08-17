
using RTSEngine.Game;

namespace RTSEngine.Controls
{
    public interface IGameControlsManager : IPreRunGameService
    {
        bool Get(ControlType controlType, bool requireValid = false);
        bool GetDown(ControlType controlType, bool requireValid = false);
        bool GetUp(ControlType controlType, bool requireValid = false);

        bool Get(ControlType controlType, KeyBehaviour behaviour, bool requireValid = false);
    }
}
