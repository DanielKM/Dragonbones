using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.UnitExtension;

namespace RTSEngine.Movement
{
    public struct MovementSource
    {
        public bool playerCommand;

        //component that requested movement
        //the Stop() method will be called on all IEntityTargetComponent components attached to moving entity when movement starts, except this one.
        public IEntityTargetComponent component;

        //IAddableUnit component that initiated movement and the position it wants to add the unit at.
        //if assigned, the entity's movement goal will be to get added to this component.
        public IAddableUnit targetAddableUnit;
        public Vector3 targetAddableUnitPosition;

        /// <summary>
        /// True when the movement is part of an attack-move command chain that was initiated by the player.
        /// </summary>
        public bool isAttackMove;
        /// <summary>
        /// True when the unit attack component moves the unit after it finishes attacking its current target when attack-move was enabled by the player for the unit in a previous movement command.
        /// </summary>
        public bool isOriginalAttackMove;
    }
}
