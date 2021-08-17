using System.Collections.Generic;

using RTSEngine.ResourceExtension;
using RTSEngine.Game;

namespace RTSEngine.UI
{
    public interface IGameUIManager: IPreRunGameService, IMonoBehaviour
    {
        bool HasPriority(IGameService testService);
        bool HasPriority(UIPriority testPriority);

        bool PrioritizeServiceUI(IGameService service);
        bool PrioritizeServiceUI(UIPriority newPriority);

        bool DeprioritizeServiceUI(IGameService service);
        bool DeprioritizeServiceUI(UIPriority removedPriority);
    }
}