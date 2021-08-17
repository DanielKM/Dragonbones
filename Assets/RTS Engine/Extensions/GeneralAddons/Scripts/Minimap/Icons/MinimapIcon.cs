using RTSEngine.Effect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace RTSEngine.Minimap.Icons
{
    /// <summary>
    /// Assigned to an entity to represent its icon on the minimap.
    /// </summary>
    public class MinimapIcon : EffectObject, IMinimapIcon
    {
        private Renderer iconRenderer;
        [SerializeField, Tooltip("Index of the material in the icon renderer to be colored.")]
        private int materialID = 0;

        /// <summary>
        /// Initializes the MinimapIcon component
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            iconRenderer = GetComponent<Renderer>();

            Assert.IsNotNull(iconRenderer, 
                $"[MinimapIcon] A {typeof(Renderer).Name} component must be attache to the minimap icon!");
        }

        /// <summary>
        /// Updates the color of the minimap icon.
        /// </summary>
        public void SetColor (Color color)
        {
            iconRenderer.materials[materialID].color = color;
        }

        /// <summary>
        /// Shows/hides the minimap icon renderer.
        /// </summary>
        public void Toggle(bool enable)
        {
            iconRenderer.enabled = enable;
        }
    }
}