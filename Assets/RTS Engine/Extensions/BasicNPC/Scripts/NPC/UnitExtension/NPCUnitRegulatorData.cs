using UnityEngine;

namespace RTSEngine.NPC.UnitExtension
{
    [CreateAssetMenu(fileName = "NewUnitRegulatorData", menuName = "RTS Engine/NPC/Basic NPC/NPC Unit Regulator Data", order = 52)]
    public class NPCUnitRegulatorData : NPCRegulatorData
    {
        [SerializeField, Tooltip("Instances of this unit amount to available population slots target ratio.")]
        private FloatRange ratioRange = new FloatRange(0.1f, 0.2f);
        public float Ratio => ratioRange.RandomValue;
    }
}
