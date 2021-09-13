using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.SpellCastExtension
{
    public class SpellCastManager : MonoBehaviour, ISpellCastManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(ISpell), sameScene: true), Tooltip("Prespawned free spells in the current map scene.")]
        private GameObject[] preSpawnedFreeSpells = new GameObject[0];

        private List<ISpell> freeSpells = new List<ISpell>();
        public IEnumerable<ISpell> FreeSpells => freeSpells;

        [SerializeField, Tooltip("Selection and minimap color that all free spells use.")]
        private Color freeSpellColor = Color.black; 
        public Color FreeSpellColor => freeSpellColor;

        // Borders
        // In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        public int LastBorderSortingOrder { private set; get; }
        private List<ISpellRange> allSpellRanges = new List<ISpellRange>();
        public IEnumerable<ISpellRange> AllSpellRanges => allSpellRanges;
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

            freeSpells = new List<ISpell>();

            this.gameMgr.GameStartRunning += HandleGameStartRunning;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
        }

        private void OnDisable()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
        }

        public void HandleGameStartRunning(IGameManager source, EventArgs args)
        {
            freeSpells.AddRange(preSpawnedFreeSpells.Select(spell => spell.GetComponent<ISpell>()));

            // Activate free spells after all faction slots are initialized.
            foreach (ISpell spell in freeSpells)
                spell.Init(
                    gameMgr,
                    new InitSpellParameters
                    {
                        free = true,
                        factionID = -1,

                        setInitialHealth = false
                    });

            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }
        #endregion

        #region Handling Events: Monitoring Free Spells
        private void HandleEntityFactionUpdateStartGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (updatedInstance.Type == EntityType.spell && updatedInstance.IsFree)
                freeSpells.Remove(updatedInstance as ISpell);
        }
        #endregion

        #region Creating Spells
        public ErrorMessage CreatePlacedSpell(ISpell placementInstance, Vector3 spawnPosition, Quaternion spawnRotation, InitSpellParameters initParams)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    isSourcePrefab = true,

                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.spell,

                    sourcePosition = spawnPosition,
                    opPosition = spawnRotation.eulerAngles,

                    code = JsonUtility.ToJson(initParams),

                    playerCommand = initParams.playerCommand
                },
                source: placementInstance,
                target: null //tlc
                );
        }

        public ISpell CreatePlacedSpellLocal(ISpell spellPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitSpellParameters initParams)
        {
            ISpell newSpell = Instantiate(spellPrefab.gameObject, spawnPosition, spawnRotation).GetComponent<ISpell>();

            newSpell.Init(gameMgr, initParams);

            return newSpell;
        }
        #endregion
    }
}
