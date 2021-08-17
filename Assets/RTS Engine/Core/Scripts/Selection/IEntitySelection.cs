using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UI;

namespace RTSEngine.Selection
{
    public interface IEntitySelection : IMonoBehaviour
    {
        IEntity Entity { get; }

        bool IsActive { get; }
        bool SelectOwnerOnly { get; set; }
        bool CanSelect { get; }
        bool IsSelected { get; }

        event CustomEventHandler<IEntity, EventArgs> Selected;
        event CustomEventHandler<IEntity, EventArgs> Deselected;

        void OnSelected();
        void OnDeselected();

        void OnAwaitingTaskAction(EntityComponentTaskUIAttributes taskAttributes);
        void OnDirectAction();
        bool IsSelectionCollider(Collider collider);
    }
}
