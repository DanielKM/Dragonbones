using RTSEngine.Entities;
using System;

namespace RTSEngine.Event
{
    public class EntityEventArgs<T> : EventArgs where T : IEntity
    {
        public T Entity { private set; get; }

        public EntityEventArgs(T entity)
        {
            this.Entity = entity;
        }
    }
}
