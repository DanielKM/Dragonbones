using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    public interface IMovementFormationHandler : IMonoBehaviour
    {
        MovementFormationType FormationType { get; }
        MovementFormationType FallbackFormationType { get; }

        int MaxEmptyAttempts { get; }

        void Init(IGameManager gameMgr);

        ErrorMessage GeneratePathDestinations(PathDestinationInputData input, ref int amount, ref float offset, ref List<Vector3> pathDestinations, out int generatedAmount);
    }
}
