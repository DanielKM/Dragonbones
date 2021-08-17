using RTSEngine.Entities;

namespace RTSEngine.Selection
{
    public class ResourceSelection : EntitySelection
    {
        #region Attributes
        protected IResource resource { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            resource = Entity as IResource;
        }
        #endregion
    }
}