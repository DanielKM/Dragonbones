using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Upgrades;
using RTSEngine.Logging;

namespace RTSEngine.UI
{
    public class GameUITextDisplayManager : MonoBehaviour, IGameUITextDisplayManager
    {
        #region Attributes
        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>(); 
        }
        #endregion

        #region Faction Entity Requirements
        public virtual bool FactionEntityRequirementsToString(IEnumerable<FactionEntityRequirement> reqs, out string text)
        {
            text = String.Empty;

            if (!reqs.IsValid() || !reqs.Any())
                return false;

            List<FactionEntityRequirement> requirementList = reqs.ToList();

            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < requirementList.Count; i++)
            {
                FactionEntityRequirement req = requirementList[i];

                string reqAmountColored = req.TestFactionEntityRequirement(gameMgr.LocalFactionSlot.FactionMgr)
                    ? $"<color=green>{req.amount}</color>"
                    : $"<color=red>{req.amount}</color>";

                builder.Append($"<b>{req.name}</b>: {reqAmountColored}");

                if (i < requirementList.Count - 1)
                    builder.Append(" -");
            }

            text = builder.ToString();

            return true;
        }
        #endregion

        #region Resources
        public virtual bool ResourceTypeValueToString (ResourceTypeValue value, out string text)
        {
            text = value.amount != 0
                ? value.amount.ToString()
                : value.capacity.ToString();

            return true;
        }

        public virtual bool ResourceInputToString(IEnumerable<ResourceInput> resourceInputs, out string text)
        {
            text = String.Empty;

            if (!resourceInputs.IsValid() || !resourceInputs.Any())
                return false;

            List<ResourceInput> resourceInputList = resourceInputs.ToList();

            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < resourceInputList.Count; i++)
            {
                ResourceInput input = resourceInputList[i];

                if (!input.type.IsValid())
                    continue;

                ResourceTypeValueToString(input.value, out string inputAmountText);

                string inputAmountTextColored = resourceMgr.HasResources(input, gameMgr.LocalFactionSlot.ID)
                    ? $"<color=green>{inputAmountText}</color>"
                    : $"<color=red>{inputAmountText}</color>";

                builder.Append($"<b>{input.type.DisplayName}</b>: {inputAmountTextColored}");

                if (i < resourceInputList.Count - 1)
                    builder.Append(" -");
            }

            text = builder.ToString();

            return true;
        }
        #endregion

        #region Tasks
        public virtual bool BuildingCreationTaskToString(BuildingCreationTask creationTask, out string text)
            => EntityComponentTaskInputToText(creationTask, creationTask?.Prefab, out text);

        public virtual bool UnitCreationTaskToString(UnitCreationTask creationTask, out string text)
            => EntityComponentTaskInputToText(creationTask, creationTask?.Prefab, out text);

        public virtual bool UpgradeTaskToString(UpgradeTask upgradeTask, out string text)
            => EntityComponentTaskInputToText(
                upgradeTask,
                (upgradeTask.Prefab as EntityUpgrade)?.UpgradeTarget,
                out text);

        public virtual bool EntityComponentTaskInputToText(IEntityComponentTaskInput taskInput, IEntity targetPrefab, out string text)
        {
            text = String.Empty;

            if(!taskInput.IsValid())
                return false;

            StringBuilder builder = new StringBuilder();

            if (EntityComponentTaskToText(taskInput.Data, out string taskDescription))
            {
                builder.AppendLine(taskDescription);
                builder.AppendLine();
            }
            if (EntityDescriptionToText(targetPrefab, out string targetPrefabDescription))
            {
                builder.AppendLine(targetPrefabDescription);
                builder.AppendLine();
            }

            if (ResourceInputToString(taskInput.RequiredResources, out string resourceInputText))
                builder.AppendLine(resourceInputText);
            if (FactionEntityRequirementsToString(taskInput.FactionEntityRequirements, out string factionReqsText))
                builder.AppendLine(factionReqsText);

            text = builder.ToString();

            return true;
        }

        public virtual bool EntityComponentTaskToText(EntityComponentTaskUIData taskData, out string text)
        {
            text = taskData.description;

            return true;
        }
        #endregion

        #region Entities
        public virtual bool EntityDescriptionToText (IEntity entity, out string text)
        {
            text = String.Empty;

            if(!entity.IsValid())
                return false;

            text = entity.Description;

            return true;
        }

        public virtual bool EntityNameToText (IEntity entity, out string text)
        {
            text = String.Empty;

            if(!entity.IsValid())
                return false;

            text = entity.Name;

            return true;
        }
        #endregion

        #region PlayerErrorMessage
        public virtual bool PlayerErrorMessageToString(PlayerErrorMessageWrapper msgWrapper, out string text)
        {
            text = "";

            switch(msgWrapper.message)
            {
                // Movement
                case ErrorMessage.mvtTargetPositionNotFound:
                case ErrorMessage.mvtPositionMarkerReserved:
                case ErrorMessage.mvtPositionNavigationOccupied:
                case ErrorMessage.mvtPositionObstacleReserved:
                    text = "Unable to find movement target position!";
                    break;

                // IEntityTargetComponent
                case ErrorMessage.entityCompTargetOutOfRange:
                    text = "Target is out of range!";
                    break;

                // Health
                case ErrorMessage.healthtMaxReached:
                    text = "Maximum health reached!";
                    break;
                case ErrorMessage.healthLow:
                    text = "Health is low!";
                    break;

                // Resources
                case ErrorMessage.resourceTargetOutsideTerritory:
                    text = "Target resource is outside your faction's territory!";
                    break;

                // WorkerManager
                case ErrorMessage.workersMaxAmountReached:
                    text = "Maximum workers amount reached!";
                    break;

                // Rallypoint
                case ErrorMessage.rallypointTargetNotInRange:
                    text = "Target is out of range for the rallypoint!";
                    break;
                case ErrorMessage.rallypointTerrainAreaMismatch:
                    text = "Terrain area is not suitable for the rallypoint!";
                    break;

                // Dropoff
                case ErrorMessage.dropoffTargetMissing:
                    text = "No dropoff target to drop resources!";
                    break;

                // Faction
                case ErrorMessage.factionMismatch:
                    text = "Factions do not match!";
                    break;
                case ErrorMessage.factionLimitReached:
                    text = "Factions limit for the entity has been reached!";
                    break;
                case ErrorMessage.factionIsFriendly:
                    text = "Target faction is a friendly faction!";
                    break;

                // Task/Action
                case ErrorMessage.taskMissingFactionEntityRequirements:
                    text = "Task faction entity requirements are missing!";
                    break;
                case ErrorMessage.taskMissingResourceRequirements:
                    text = "Task required resources are missing!";
                    break;

                // IUnitCarrier
                case ErrorMessage.carrierCapacityReached:
                    text = "Unit carrier reached maximum capacity!";
                    break;

                // Attack
                case ErrorMessage.attackTypeActive:
                    text = "Attack type is already active!";
                    break;
                case ErrorMessage.attackTypeLocked:
                    text = "Attack type is locked!";
                    break;
                case ErrorMessage.attackTypeInCooldown:
                    text = "Attack type is in cooldown!";
                    break;
                case ErrorMessage.attackTargetRequired:
                    text = "Attack type requires a valid target!";
                    break;
                case ErrorMessage.attackPositionOutOfRange:
                case ErrorMessage.attackTargetOutOfRange:
                    text = "Attack target is out of range!";
                    break;
                case ErrorMessage.attackPositionNotFound:
                    text = "Unable to find attack position!";
                    break;

                // Upgrade
                case ErrorMessage.upgradeLaunched:
                    text = "Upgrade has been already launched";
                    break;

                // Unit Creator
                case ErrorMessage.unitCreatorMaxLaunchTimesReached:
                    text = "Unit creator reached maximum launch times!";
                    break;
                case ErrorMessage.unitCreatorMaxActiveInstancesReached:
                    text = "Unit creator reached maximum active created units!";
                    break;

                // Game
                case ErrorMessage.gameFrozen:
                    text = "Game is frozen";
                    break;
                case ErrorMessage.gamePeaceTimeActive:
                    text = "Game is still in peace time!";
                    break;

                default:
                    return false;
            }

            return true;
        }
        #endregion

    }
}
