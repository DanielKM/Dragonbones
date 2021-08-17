using System.Collections.Generic;

using RTSEngine.Event;

namespace RTSEngine.EntityComponent
{
    public interface IEntityComponentTracker<T> where T : IEntityComponent
    {
        IEnumerable<T> Components { get; }

        event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentAdded;
        event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentUpdated;
        event CustomEventHandler<IEntityComponentTracker<T>, EntityComponentEventArgs<T>> ComponentRemoved;

        void Disable();
    }
}
