using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct MultipleSelectionTaskUIAttributes : ITaskUIAttributes
    {
        public MultipleSelectionTaskUIData data;

        public IEnumerable<IEntity> selectedEntities;
    }
}
