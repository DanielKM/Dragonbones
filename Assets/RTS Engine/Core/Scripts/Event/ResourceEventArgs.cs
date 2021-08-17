using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Event
{
    public class ResourceEventArgs : EventArgs
    {
        public IResource Resource { private set; get; }

        public ResourceEventArgs(IResource resource)
        {
            this.Resource = resource;
        }
    }
}
