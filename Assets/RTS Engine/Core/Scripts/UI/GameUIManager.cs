using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.UI
{
    public partial class GameUIManager : MonoBehaviour, IGameUIManager
    {
        #region Attributes
        private LinkedList<UIPriority> priorityUIServices = new LinkedList<UIPriority>();
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
        }

        private void OnDestroy()
        {
            priorityUIServices = null;
        }
        #endregion

        #region Handling Game Service UI Priority
        public bool HasPriority(IGameService testService)
            => HasPriority(new UIPriority
            {
                service = testService,
                condition = null
            });

        public bool HasPriority(UIPriority testPriority)
        {
            if (!priorityUIServices.Any())
                return true;

            foreach (UIPriority priority in priorityUIServices)
                // As soon as the first one of of the current registered priority UI services have their condition fulfilled then it has priority
                // In that case make sure that the prioritized UI service matches the test priority service and condition
                if (!priority.condition.IsValid() || priority.condition() == true)
                    return testPriority.IsMatch(priority);

            return true;
        }

        public bool PrioritizeServiceUI(IGameService service)
            => PrioritizeServiceUI(new UIPriority
            {
                service = service,
                condition = null
            });

        public bool PrioritizeServiceUI(UIPriority newPriority)
        {
            // Make sure that no priority UI services is the same as the one we are trying to add now
            if (priorityUIServices.Where(priority => priority.IsMatch(newPriority)).Any())
                return false;

            priorityUIServices.AddFirst(newPriority);
            return true;
        }

        public bool DeprioritizeServiceUI(UIPriority removedPriority)
        {
            // As long as there are services that match with the removed priority, remove them and reassign the linked list.
            var matchPriorities = priorityUIServices.Where(priority => priority.IsMatch(removedPriority));
            if (!matchPriorities.Any())
                return false;

            priorityUIServices = new LinkedList<UIPriority>(priorityUIServices.Except(matchPriorities));
            return true;
        }

        public bool DeprioritizeServiceUI(IGameService removedService)
            => DeprioritizeServiceUI(new UIPriority
            {
                service = removedService,
                condition = null
            });
        #endregion
    }
}