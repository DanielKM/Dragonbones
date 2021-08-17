using System.Collections;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Selection
{
    public class EntitySelectionRenderer : MonoBehaviour, IEntitySelectionMarker, IEntityPreInitializable
    {
        #region Class Attributes
        [SerializeField, Tooltip("Renderer used to display the selection texture of an entity.")]
        private Renderer selectionRenderer = null;
        [SerializeField, Tooltip("Index of the material assigned to the renderer to be colored with the faction colors.")]
        private int materialID = 0;

        private Coroutine flashCoroutine;

        private IEntitySelection entitySelection;

        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            this.entitySelection = entity.Selection;

            if (!logger.RequireValid(selectionRenderer,
                $"[{GetType().Name} - {entity.Code}] The 'Selection Renderer' field must be assigned!"))
                return;

            Disable();
            StopFlash();
        }
        #endregion

        #region Enabling/Disabling Selection Renderer
        public void Enable (Color color)
        {
            selectionRenderer.materials[materialID].color = color;
            selectionRenderer.enabled = true;
        }

        public void Enable ()
        {
            selectionRenderer.materials[materialID].color = entitySelection.Entity.SelectionColor;
            selectionRenderer.enabled = true;
        }

        public void Disable ()
        {
            selectionRenderer.enabled = false;
        }
        #endregion

        #region Flashing Selection Renderer
        public void StartFlash (float totalDuration, float cycleDuration, Color flashColor)
        {
            StopFlash();

            selectionRenderer.materials[materialID].color = flashColor;
            flashCoroutine = StartCoroutine(Flash(totalDuration, cycleDuration));
        }

        public void StopFlash ()
        {
            if (flashCoroutine.IsValid())
                StopCoroutine(flashCoroutine);

            // if the entity was selected before the selection flash.
            // Enable the selection plane with the actual entity's colors again
            if (entitySelection.IsSelected) 
                Enable(); 
            else
                Disable();
        }

        private IEnumerator Flash(float totalDuration, float cycleDuration)
        {
            while(true)
            {
                yield return new WaitForSeconds(cycleDuration);

                selectionRenderer.enabled = !selectionRenderer.enabled;

                totalDuration -= cycleDuration;
                if (totalDuration <= 0.0f)
                    yield break;
            }
        }
        #endregion
    }
}
