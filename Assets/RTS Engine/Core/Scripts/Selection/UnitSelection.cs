using RTSEngine.Entities;

namespace RTSEngine.Selection
{
    public class UnitSelection : EntitySelection
    {
        #region Attributes
        protected IUnit unit { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        { 
            unit = Entity as IUnit;
        }
        #endregion
    }
}
