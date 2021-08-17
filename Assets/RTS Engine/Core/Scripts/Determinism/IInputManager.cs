using RTSEngine.Entities;
using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.Determinism
{
    public interface IInputManager : IPreRunGameService
    {
        int RegisterEntity(IEntity newEntity);

        ErrorMessage SendInput(CommandInput newInput, IEntity source, IEntity target);
        ErrorMessage SendInput(CommandInput newInput, IEnumerable<IEntity> source, IEntity target);
        ErrorMessage SendInput(CommandInput newInputAction);

        void LaunchInput(CommandInput input);
        void LaunchInput(IEnumerable<CommandInput> inputs);

        IntValues ToIntValues(int int1);
        IntValues ToIntValues(int int1, int int2);
    }
}