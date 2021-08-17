using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.Demo
{
    public class UnitDeathEffect : MonoBehaviour, IEntityPostInitializable
    {
        private IUnit unit;

        [SerializeField, Tooltip("List of the rigidbodies that will be manipulated to make the unit death effect.")]
        private Rigidbody[] rigidbodies = new Rigidbody[0];

        [SerializeField, Tooltip("Intensity of the force to be applied to the above rigidbodies when the unit is dead.")]
        private FloatRange forceIntensityRange = new FloatRange(-2.5f, 2.5f);

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            unit = entity as IUnit;

            //enable kinematic mode, disable gravity and enable trigger
            foreach (Rigidbody r in rigidbodies)
            {
                r.isKinematic = true;
                r.useGravity = false;
                r.gameObject.GetComponent<Collider>().isTrigger = true;
                r.gameObject.GetComponent<Collider>().enabled = false;
            }

            unit.Health.EntityDead += HandleEntityDead;
        }

        public void Disable()
        {
            unit.Health.EntityDead -= HandleEntityDead;
        }

        private void HandleEntityDead(IEntity sender, DeadEventArgs args)
        {
            if (unit.AnimatorController.Animator.IsValid())
                unit.AnimatorController.Animator.enabled = false;

            //disable kinematic mode and enable gravity
            foreach (Rigidbody r in rigidbodies)
            {
                r.isKinematic = false;
                r.useGravity = true;
                r.gameObject.GetComponent<Collider>().enabled = true;
                r.gameObject.GetComponent<Collider>().isTrigger = false;

                //add force to the model's parts
                r.AddForce(new Vector3(forceIntensityRange.RandomValue, forceIntensityRange.RandomValue, forceIntensityRange.RandomValue), ForceMode.Impulse);
            }

            unit.Health.EntityDead -= HandleEntityDead;
        }
    }
}
