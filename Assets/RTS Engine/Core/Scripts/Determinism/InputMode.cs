namespace RTSEngine.Determinism
{
    public enum InputMode
    {
        create,

        // IEntity related
        entity,
        factionEntity,
        building,
        resource,
        unit,

        entityGroup,

        setFaction,
        setComponentActive,
        setComponentTargetFirst,
        setComponentTarget,
        launchComponentAction,
        movement,
        attack,

        // Health
        health,
        healthSetMax,
        healthAddCurr,
        healthDestroy,

        // Faction
        faction,
        factionDestroy,

        // master related:
        master,
        setTimeModifier,

        custom,
    }; 
}