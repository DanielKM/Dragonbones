using RTSEngine.Entities;
using RTSEngine.NPC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Logging.NPC
{
    [System.Serializable]
    public struct NPCActiveFactionEntityRegulatorLogData
    {
        public string prefabCode;

        [Space()]
        public int curr;

        [Space()]
        public int pending;
        public int maxPending;

        [Space()]
        public int target;

        [Space()]
        public int min;
        public int max;

        [Space()]
        public string[] creators;
        public float spawnTimer;

        public NPCActiveFactionEntityRegulatorLogData(INPCRegulator regulator, float spawnTimer = -1.0f, string[] creators = null)
        {
            prefabCode = regulator.Prefab.Code;

            curr = regulator.Count;

            pending = regulator.CurrPendingAmount;
            maxPending = regulator.MaxPendingAmount;

            target = regulator.TargetCount;

            min = regulator.MinTargetAmount;
            max = regulator.MaxPendingAmount;

            this.creators = creators;
            this.spawnTimer = spawnTimer;
        }
    }
}

