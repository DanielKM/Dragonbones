using System;

using RTSEngine.Game;

namespace RTSEngine.UI
{
    public struct UIPriority
    {
        public IGameService service;
        public Func<bool> condition;

        public bool IsMatch(UIPriority testPriority)
        {
            return testPriority.service == service
                && (!testPriority.condition.IsValid() || testPriority.condition == condition);
        }
    }
}