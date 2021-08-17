using RTSEngine.BuildingExtension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Event
{
    public class BorderEventArgs : EventArgs
    {
        public IBorder Border { private set; get; }

        public BorderEventArgs(IBorder border)
        {
            this.Border = border;
        }
    }
}
