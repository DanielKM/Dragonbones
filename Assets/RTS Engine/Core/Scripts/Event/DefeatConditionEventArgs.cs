using System;

using RTSEngine.Game;

namespace RTSEngine.Event
{
    public class DefeatConditionEventArgs : EventArgs
    {
        public DefeatConditionType Type { private set; get; }

        public DefeatConditionEventArgs(DefeatConditionType type)
        {
            this.Type = type;
        }
    }
}
