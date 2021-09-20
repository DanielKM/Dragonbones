using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.ResourceExtension;
using System.Collections.Generic;

namespace RTSEngine.UI
{
    public interface IGameUITextDisplayManager : IPreRunGameService
    {
        bool UnitCreationTaskToString(UnitCreationTask creationTask, out string text);
        bool UpgradeTaskToString(UpgradeTask upgradeTask, out string text);
        bool BuildingCreationTaskToString(BuildingCreationTask creationTask, out string text);
        bool EntityComponentTaskInputToText(IEntityComponentTaskInput taskInput, IEntity targetPrefab, out string text);
        bool EntityComponentTaskToText(EntityComponentTaskUIData taskData, out string text);

        bool EntityDescriptionToText(IEntity entity, out string text);
        bool EntityNameToText(IEntity entity, out string text);

        bool ResourceInputToString(IEnumerable<ResourceInput> resourceInputSet, out string text);

        bool PlayerErrorMessageToString(PlayerErrorMessageWrapper msgWrapper, out string text);
    }
}