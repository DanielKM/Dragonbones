using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.UnitExtension
{
    public class UnitManager : MonoBehaviour, IUnitManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IUnit), sameScene: true), Tooltip("Prespawned free units in the current map scene.")]
        private GameObject[] preSpawnedFreeUnits = new GameObject[0];

        private List<IUnit> freeUnits = null;
        public IEnumerable<IUnit> FreeUnits => freeUnits;

        [SerializeField, Tooltip("Selection and minimap color that all free units use.")]
        private Color freeUnitColor = Color.black;
        public Color FreeUnitColor => freeUnitColor;

        [SerializeField, EnforceType(prefabOnly: true), Tooltip("The default animator controller to be used by units when the do not have a custom one assigned.")]
        private AnimatorOverrideController defaultAnimController = null;
        public AnimatorOverrideController DefaultAnimController => defaultAnimController;

        // All active units are tracked via this list.
        private List<IUnit> allUnits = null;
        public IEnumerable<IUnit> AllUnits => allUnits;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();
            this.inputMgr = gameMgr.GetService<IInputManager>();

            allUnits = new List<IUnit>();
            freeUnits = new List<IUnit>();

            this.gameMgr.GameStartRunning += HandleGameStartRunning;

            globalEvent.UnitInitiatedGlobal += HandleUnitInitiatedGlobal;
            globalEvent.UnitDeadGlobal += HandleUnitDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
        }

        private void OnDestroy()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;

            globalEvent.UnitInitiatedGlobal -= HandleUnitInitiatedGlobal;
            globalEvent.UnitDeadGlobal -= HandleUnitDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
        }

        public void HandleGameStartRunning(IGameManager source, EventArgs args)
        {
            freeUnits.AddRange(preSpawnedFreeUnits.Select(unit => unit.GetComponent<IUnit>()));

            // Activate free units
            foreach (IUnit unit in preSpawnedFreeUnits.Select(unit => unit.GetComponent<IUnit>()).ToList())
                unit.Init(gameMgr, new InitUnitParameters
                {
                    free = true,
                    factionID = -1,

                    setInitialHealth = false,

                    rallypoint = null,
                    gotoPosition = unit.transform.position,
                });

            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }
        #endregion

        #region Handling Events: Monitoring Free Units
        private void HandleUnitInitiatedGlobal(IUnit unit, EventArgs e)
        {
            allUnits.Add(unit);

            if (unit.IsFree)
                freeUnits.Add(unit);
        }

        private void HandleUnitDeadGlobal(IUnit unit, DeadEventArgs e)
        {
            allUnits.Remove(unit);

            if (unit.IsFree)
                freeUnits.Remove(unit);
        }

        private void HandleEntityFactionUpdateStartGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (updatedInstance.Type == EntityType.unit && updatedInstance.IsFree) //if the source unit was free
                freeUnits.Remove(updatedInstance as Unit);
        }
        #endregion

        #region Creating Units
        public ErrorMessage CreateUnit(IUnit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitUnitParameters initParams)
        {
            //send input action to the input manager
            return inputMgr.SendInput(new CommandInput
            {
                isSourcePrefab = true,

                sourceMode = (byte)InputMode.create,
                targetMode = (byte)InputMode.unit,

                sourcePosition = spawnPosition,
                opPosition = spawnRotation.eulerAngles,

                code = JsonUtility.ToJson(
                    new InitUnitParametersInput 
                    {
                        factionID = initParams.factionID,
                        free = initParams.free,

                        setInitialHealth = initParams.setInitialHealth,
                        initialHealth = initParams.initialHealth,

                        rallypointCode = initParams.rallypoint?.Code,
                        creatorEntityComponentCode = initParams.creatorEntityComponent?.Code,

                        gotoPosition = initParams.gotoPosition,

                        playerCommand = initParams.playerCommand,
                    }),

                playerCommand = false 
            },
            source: unitPrefab,
            target: initParams.rallypoint?.Entity);
        }

        public IUnit CreateUnitLocal(IUnit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitUnitParameters initParams)
        {
            IUnit newUnit = Instantiate(unitPrefab.gameObject, spawnPosition, spawnRotation).GetComponent<IUnit>();

            newUnit.Init(gameMgr, initParams);

            return newUnit;
        }
        #endregion
    }
}