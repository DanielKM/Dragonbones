using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System;

namespace RTSEngine.Event
{
    public class TargetDataEventArgs : EventArgs
    {
        public TargetData<IEntity> Data { private set; get; }

        public TargetDataEventArgs (TargetData<IEntity> data)
        {
            this.Data = data;
        }
    }
}
