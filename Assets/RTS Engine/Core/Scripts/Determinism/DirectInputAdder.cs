using RTSEngine.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Determinism
{
    public class DirectInputAdder : IInputAdder
    {
        protected IInputManager inputMgr { private set; get; }

        public DirectInputAdder(IGameManager gameMgr)
        {
            this.inputMgr = gameMgr.GetService<IInputManager>(); 
        }

        public void AddInput(CommandInput input)
        {
            inputMgr.LaunchInput(input);
        }

        public void AddInput(IEnumerable<CommandInput> inputs)
        {
            inputMgr.LaunchInput(inputs);
        }
    }
}
