namespace RTSEngine.Entities
{
    [System.Serializable]
    public class FactionEntityTargetPicker : TargetPicker<IFactionEntity, CodeCategoryField>
    {
        /// <summary>
        /// Is the FactionEntity instance defined in the CodeCategoryField type of list?
        /// </summary>
        /// <param name="factionEntity">FactionEntity instance to test.</param>
        /// <returns>True if the faction entity's code/category is defined in one of the CodeCategoryField entries in the list, otherwise false.</returns>
        protected override bool IsInList(IFactionEntity factionEntity)
        {
            return options.Contains(factionEntity.Code, factionEntity.Category);
        }
    }
}
