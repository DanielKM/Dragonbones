using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public class BuildingManager : MonoBehaviour, IBuildingManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IBuilding), sameScene: true), Tooltip("Prespawned free buildings in the current map scene.")]
        private GameObject[] preSpawnedFreeBuildings = new GameObject[0];

        private List<IBuilding> freeBuildings = new List<IBuilding>();
        public IEnumerable<IBuilding> FreeBuildings => freeBuildings;

        [SerializeField, Tooltip("Selection and minimap color that all free buildings use.")]
        private Color freeBuildingColor = Color.black; 
        public Color FreeBuildingColor => freeBuildingColor;

        // Borders
        // In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        public int LastBorderSortingOrder { private set; get; }
        private List<IBorder> allBorders = new List<IBorder>();
        public IEnumerable<IBorder> AllBorders => allBorders;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.inputMgr = gameMgr.GetService<IInputManager>();

            freeBuildings = new List<IBuilding>();

            this.gameMgr.GameStartRunning += HandleGameStartRunning;

            globalEvent.BorderActivatedGlobal += HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
        }

        private void OnDisable()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;

            globalEvent.BorderActivatedGlobal -= HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
        }

        public void HandleGameStartRunning(IGameManager source, EventArgs args)
        {
            freeBuildings.AddRange(preSpawnedFreeBuildings.Select(building => building.GetComponent<IBuilding>()));

            // Activate free buildings after all faction slots are initialized.
            foreach (IBuilding building in freeBuildings)
                building.Init(
                    gameMgr,
                    new InitBuildingParameters
                    {
                        free = true,
                        factionID = -1,

                        setInitialHealth = false,

                        buildingCenter = null,
                    });

            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }
        #endregion

        #region Handling Events: Monitoring Borders
        private void HandleBorderActivatedGlobal(IBorder border, EventArgs e)
        {
            allBorders.Add(border);
            LastBorderSortingOrder--;
        }

        private void HandleBorderDisabledGlobal(IBorder border, EventArgs e) => allBorders.Remove(border);
        #endregion

        #region Handling Events: Monitoring Free Buildings
        private void HandleEntityFactionUpdateStartGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (updatedInstance.Type == EntityType.building && updatedInstance.IsFree)
                freeBuildings.Remove(updatedInstance as IBuilding);
        }
        #endregion

        #region Creating Buildings
        public ErrorMessage CreatePlacedBuilding(IBuilding placementInstance, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    isSourcePrefab = true,

                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.building,

                    sourcePosition = spawnPosition,
                    opPosition = spawnRotation.eulerAngles,

                    code = JsonUtility.ToJson(initParams),

                    playerCommand = initParams.playerCommand
                },
                source: placementInstance,
                target: initParams.buildingCenter?.Building);
        }

        public IBuilding CreatePlacedBuildingLocal(IBuilding buildingPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams)
        {
            IBuilding newBuilding = Instantiate(buildingPrefab.gameObject, spawnPosition, spawnRotation).GetComponent<IBuilding>();

            newBuilding.Init(gameMgr, initParams);

            return newBuilding;
        }
        #endregion
    }
}
