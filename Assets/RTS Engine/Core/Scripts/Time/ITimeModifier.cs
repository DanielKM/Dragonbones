using RTSEngine.Event;
using RTSEngine.Game;
using System;
using System.Collections.Generic;

namespace RTSEngine.Determinism
{
    public interface ITimeModifier : IPreRunGameService
    {
        event CustomEventHandler<ITimeModifier, EventArgs> ModifierUpdated;

        void ResetModifier(bool playerCommand);

        ErrorMessage SetModifier(float newModifier, bool playerCommand);
        ErrorMessage SetModifierLocal(float newModifier, bool playerCommand);

        void AddTimer(GlobalTimeModifiedTimer timeModifiedTimer, Action removalCallback);
        bool RemoveTimer(GlobalTimeModifiedTimer timeModifiedTimer);
        bool RemoveTimer(KeyValuePair<GlobalTimeModifiedTimer, Action> timer);
    }
}