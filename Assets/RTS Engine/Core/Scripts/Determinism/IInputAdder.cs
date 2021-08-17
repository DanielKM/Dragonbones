using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Determinism
{
    public interface IInputAdder
    {
        void AddInput(CommandInput input);
        void AddInput(IEnumerable<CommandInput> inputs);
    }
}
