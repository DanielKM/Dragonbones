namespace RTSEngine
{
    public enum ErrorMessage
    {
        // -----------------------------------------
        // EntitySelectionGroup
        unitGroupSet,
        unitGroupEmpty,
        unitGroupSelected,
        // -----------------------------------------

        none, // NO ERROR MESSAGE

        inactive, 
        undefined,
        disabled,

        invalid,
        blocked,
        locked,
        failed,

        noAuthority,

        // IEntity
        uninteractable,
        dead,
        entityCodeMismatch,

        // Movement
        mvtDisabled,
        mvtTargetPositionNotFound,
        mvtPositionMarkerReserved,
        mvtPositionNavigationOccupied,
        mvtPositionObstacleReserved,

        // IEntityTargetComponent
        entityCompTargetOutOfRange,
        entityCompTargetPickerUndefined, 

        // Search
        searchCellNotFound,
        searchTargetNotFound,
        searchAreaMissingFullAmount,

        // Health
        healthPreAddBlocked,
        healthtMaxReached,
        healthLow,
        healthNoIncrease,
        healthNoDecrease,

        // Resources
        resourceTargetOutsideTerritory,
        resourceNotCollectable,

        // WorkerManager
        workersMaxAmountReached,

        // Rallypoint 
        rallypointTargetNotInRange,
        rallypointTerrainAreaMismatch,

        // Dropoff
        dropoffTargetMissing,

        // Faction
        factionLimitReached,
        factionUnderAttack,
        factionMismatch,
        factionIsFriendly,
        factionLocked,

        // Task/Action
        taskSourceCanNotLaunch,
        taskMissingFactionEntityRequirements,
        taskMissingResourceRequirements,

        // IUnitCarrier
        carrierCapacityReached,
        carrierIdleOnlyAllowed,
        carrierAttackerNotAllowed,
        carrierMissing,
        carriableComponentMissing,

        // LOS
        LOSObstacleBlocked,
        LOSAngleBlocked,

        // Attack
        attackTypeActive,
        attackTypeLocked,
        attackTypeNotFound,
        attackTypeInCooldown,
        attackTargetNoChange,
        attackTargetRequired,
        attackTargetOutOfRange,
        attackDisabled,
        attackPositionNotFound,
        attackPositionOutOfRange,
        attackMoveToTargetOnly,
        attackTerrainDisabled,
        attackAlreadyInPosition,

        // Upgrade
        upgradeLaunched,
        upgradeTypeMismatch,

        // UnitCreator 
        unitCreatorMaxLaunchTimesReached,
        unitCreatorMaxActiveInstancesReached,

        // Terrain
        terrainHeightCacheNotFound,

        // Selection
        positionOutOfSelectionBounds,

        // Game
        gameFrozen,
        gamePeaceTimeActive,

        // Lobby
        lobbyMinSlotsUnsatisfied,
        lobbyMaxSlotsUnsatisfied,
        lobbyHostOnly,
        lobbyPlayersNotAllReady,
    }
}
