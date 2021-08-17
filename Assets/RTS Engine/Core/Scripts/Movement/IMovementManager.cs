using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    public interface IMovementManager : IPreRunGameService
    {
        float StoppingDistance { get; }

        IEffectObject MovementTargetEffect { get; }

        IMovementSystem MvtSystem { get; }

        ErrorMessage SetPathDestination(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);
        ErrorMessage SetPathDestination(IEnumerable<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, bool playerCommand);
        ErrorMessage SetPathDestinationLocal(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);
        ErrorMessage SetPathDestinationLocal(IEnumerable<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, bool playerCommand);

        ErrorMessage GeneratePathDestination(IEnumerable<IEntity> entities, Vector3 targetPosition, MovementFormationSelector formation, float offset, bool playerCommand, out List<Vector3> pathDestinations, Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null);
        ErrorMessage GeneratePathDestination(IEntity entity, Vector3 targetPosition, float offset, bool playerCommand, out List<Vector3> pathDestinations, Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null);

        bool TryGetMovablePosition(Vector3 center, float radius, LayerMask areaMask, out Vector3 movablePosition);
        bool GetRandomMovablePosition(IEntity entity, Vector3 origin, float range, out Vector3 targetPosition, bool playerCommand);

        ErrorMessage IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask navAreaMask, IEnumerable<TerrainAreaType> terrainAreas, bool playerCommand);
        ErrorMessage IsPositionClear(ref Vector3 targetPosition, IMovementComponent refMvtComp, bool playerCommand);
    }
}