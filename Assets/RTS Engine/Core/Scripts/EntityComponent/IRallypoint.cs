using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IRallypoint : IEntityTargetComponent
    {
        ErrorMessage SendAction (IUnit entity, bool playerCommand);

        void SetGotoTransformActive(bool active);
    }
}
