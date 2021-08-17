namespace RTSEngine.Attack
{
    /// <summary>
    /// random: one attack source from the below array will be randomly chosen and triggered
    /// inOrder: the attack will trigger all elements of the below array in their order.
    /// simultaneous: attack object sources will be all launched simultaneously.
    /// </summary>
    public enum AttackObjectLaunchType { random, inOrder, simultaneous }
}
