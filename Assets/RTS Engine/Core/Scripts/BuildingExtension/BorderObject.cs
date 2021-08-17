using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public class BorderObject : MonoBehaviour, IBorderObject
    {
        #region Attributes
        [SerializeField, Tooltip("What parts of the border model will be colored with the faction colors?")]
        private ColoredRenderer[] coloredRenderers = new ColoredRenderer[0];

        [SerializeField, Tooltip("The height at which the border object will be created.")]
        private float height = 20.0f;

        [SerializeField, Tooltip("The border object's scale will be equal to the size (chosen in the Border component) multiplied by this value."), Min(0.0f)]
        private float sizeMultiplier = 2.0f;
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IBorder border)
        {
            transform.position = new Vector3(transform.position.x, height, transform.position.z);

            transform.SetParent(border.Building.transform, true);

            foreach (ColoredRenderer cr in coloredRenderers)
            {
                cr.UpdateColor(border.Building.SelectionColor, gameMgr.GetService<IGameLoggingService>());
                cr.renderer.sortingOrder = border.SortingOrder;
            }

            Vector3 nextScale = Vector3.one * border.Size * sizeMultiplier;
            transform.localScale = new Vector3 (nextScale.x, transform.localScale.y, nextScale.z);

            OnInit(border);
        }

        protected virtual void OnInit(IBorder border) { }
        #endregion
    }
}
