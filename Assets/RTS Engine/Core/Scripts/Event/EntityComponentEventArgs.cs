using RTSEngine.EntityComponent;
using System;

namespace RTSEngine.Event
{
    public class EntityComponentEventArgs<T> : EventArgs where T : IEntityComponent
    {
        public T Component { private set; get; }

        public EntityComponentEventArgs(T component)
        {
            this.Component = component;
        }
    }
}
