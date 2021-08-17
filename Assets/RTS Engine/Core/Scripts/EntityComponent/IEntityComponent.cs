using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.UI;

namespace RTSEngine.EntityComponent
{
    public interface IEntityComponent : IMonoBehaviour, IEntityPostInitializable
    {
        string Code { get; }

        bool IsActive { get; }
        
        IEntity Entity { get; }

        ErrorMessage SetActive(bool active, bool playerCommand);
        ErrorMessage SetActiveLocal(bool active, bool playerCommand);

        bool OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes);

        bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes);

        ErrorMessage LaunchAction(byte actionID, TargetData<IEntity> target, bool playerCommand);
        ErrorMessage LaunchActionLocal(byte actionID, TargetData<IEntity> target, bool playerCommand);

        void HandleComponentUpgrade(IEntityComponent sourceEntityComponent);
    }
}
