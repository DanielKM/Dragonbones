using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace RTSEngine.Effect
{
    [RequireComponent(typeof(IEffectObject))]
	public class FlashEffectObject : MonoBehaviour
    {
        [SerializeField, Tooltip("How often does the effect object flash?")]
        public float cycleDuration = 0.2f;

        [SerializeField, Tooltip("The gameobject that will be disabled and enabled during the flashing of the effect object.")]
        private GameObject flashingObject = null;

        private IEffectObject effectObject;

        void Start()
        {
            effectObject = GetComponent<IEffectObject>();

            Assert.IsNotNull(effectObject,
                $"[FlashEffectObject] Component must be attached to an object that has a component that implements '{typeof(IEffectObject).Name}' attached to it!");

            Assert.IsNotNull(flashingObject,
                $"[FlashEffectObject] 'Flashing Object' field must be assigned!");

            Assert.IsTrue(flashingObject != gameObject,
                $"[FlashEffectObject] 'Flashing Object' field must be asssigned to an object other than the one where the '{typeof(IEffectObject).Name}' component is attached!");
        }

        public void EnableFlash()
        {
            InvokeRepeating("Flash", 0.0f, cycleDuration);
        }

        private void Flash()
        {
            //as long as the game object is active
            if (effectObject.State == EffectObjectState.running)
                flashingObject.SetActive(!flashingObject.activeInHierarchy);
        }

        public void DisableFlash()
        {
            CancelInvoke("Flash");
        }
    }
}