using RTSEngine.Game;

namespace RTSEngine.Task
{
    public interface ITaskManager : IPreRunGameService
    {
        EntityComponentAwaitingTask AwaitingTask { get; }
    }
}