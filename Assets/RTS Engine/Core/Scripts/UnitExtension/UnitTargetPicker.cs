using RTSEngine.Entities;

namespace RTSEngine.UnitExtension
{
    [System.Serializable]
    public class UnitTargetPicker : TargetPicker<IUnit, CodeCategoryField>
    {
        protected override bool IsInList(IUnit unit)
        {
            if (options.Contains(unit.Code, unit.Category))
                return true;

            return false;
        }
    }
}
