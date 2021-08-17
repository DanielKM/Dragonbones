using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    public struct PathDestinationInputData
    {
        public IMovementComponent refMvtComp;

        public Vector3 targetPosition;
        public Vector3 direction;

        // Maximum distance between the generated path destination and the target position
        public float maxDistance; 

        public MovementFormationSelector formationSelector;

        public System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition;

        public bool playerCommand;
    }
}
