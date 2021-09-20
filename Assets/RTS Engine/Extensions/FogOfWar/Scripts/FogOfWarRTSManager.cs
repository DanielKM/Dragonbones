using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;

using FoW;

namespace RTSEngine.FoW
{
    public class FogOfWarRTSManager : MonoBehaviour, IFogOfWarRTSManager 
    {
        [SerializeField, Tooltip("Drag and drop the FogOfWarLegacy components attached to the cameras in the map scene.")]
        private FogOfWarLegacy[] fogOfWarCameras = new FogOfWarLegacy[0];

        // FoW components:
        private FogOfWarTeam fowHandler;

        // Services
        protected IGameLoggingService logger { private set; get; }

        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();

            fowHandler = gameObject.GetComponent<FogOfWarTeam>();

            if (!logger.RequireValid(fowHandler,
              $"[{GetType().Name}] A component of type '{typeof(FogOfWarTeam).Name}' must be added to the same game object as this component!"))
                return; 

            fowHandler.team = gameMgr.LocalFactionSlotID;

            if (!logger.RequireTrue(fogOfWarCameras.Length > 0,
              $"[{GetType().Name}] No component of type '{typeof(FogOfWarLegacy).Name}' attached to a camera has been added to the 'Fog Of War Cameras' field.",
              type: LoggingType.warning)
            || !logger.RequireValid(fogOfWarCameras,
              $"[{GetType().Name}] 'Fog Of War Cameras' list field has some invalid elements."))
                return;

            foreach (FogOfWarLegacy fowCam in fogOfWarCameras)
                fowCam.team = gameMgr.LocalFactionSlotID;

            fowHandler.Reinitialize();
        }

        public bool IsInFog(Vector3 position, byte minFogStrength)
            => fowHandler.GetFogValue(position) >= minFogStrength;
    }
}
