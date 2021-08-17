using UnityEngine;
using UnityEngine.Events;

namespace RTSEngine.Health
{
    [System.Serializable]
    public class EntityHealthState
    {
        [SerializeField, Tooltip("The entity is considered in this state only if its health is inside this range.")]
        private IntRange healthRange = new IntRange(0, 100);

        public int UpperLimit => healthRange.max;
        public int LowerLimit => healthRange.min;

        // When 'upperBoundState' is set to true, it means that there is no other health state which has a higher health interval
        // In this case, we do not consider the upper bound of the interval
        public bool IsInRange(int value, bool upperBoundState = false) => value >= healthRange.min && (upperBoundState || value < healthRange.max);

        [SerializeField, Tooltip("Gameobjects to show when the entity is in this health state.")]
        private GameObject[] showChildObjects = new GameObject[0];

        [SerializeField, Tooltip("Gameobjects to hide when the entity is in this health state.")]
        private GameObject[] hideChildObjects = new GameObject[0];  

        [SerializeField, Tooltip("Event(s) triggered when the entity enters this health state.")]
        private UnityEvent triggerEvent = new UnityEvent();

        public bool Toggle(bool enable)
        {
            foreach (GameObject obj in showChildObjects)
            {
                if (!obj.IsValid())
                    return false;

                obj.SetActive(enable);
            }

            foreach (GameObject obj in hideChildObjects)
            {
                if (!obj.IsValid())
                    return false;

                obj.SetActive(!enable);
            }

            if (enable)
                triggerEvent.Invoke();

            return true;
        }
    }
}
