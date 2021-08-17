using System;

namespace RTSEngine.Event
{
    public class VisibilityEventArgs : EventArgs
    {
        public bool IsVisible { private set; get; }

        public VisibilityEventArgs(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}