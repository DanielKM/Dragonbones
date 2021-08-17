using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    [CreateAssetMenu(fileName = "NewEntityComponentTaskUIAsset", menuName = "RTS Engine/Entity Component Task UI Asset", order = 100)]
    public class EntityComponentTaskUIAsset : RTSEngineScriptableObject
    {
        [SerializeField]
        private EntityComponentTaskUIData data = new EntityComponentTaskUIData
        {
            enabled = true,
            code = "unique_code",
            description = "tooltip text",

            hideTooltipOnClick = true,
            tooltipEnabled = true,

            reloadTime = 0.1f
        };

        public EntityComponentTaskUIData Data => data;

        /// <summary>
        /// Get the unique code of the EntityComponentTaskUI instance.
        /// </summary>
        public override string Key => data.code;
    }
}
