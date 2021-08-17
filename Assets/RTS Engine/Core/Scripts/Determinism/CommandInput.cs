using System;
using UnityEngine;

namespace RTSEngine.Determinism
{
    public struct IntValues
    {
        public int Item1;
        public int Item2;
    }

    public struct CommandInput
    {
        public byte sourceMode; //input source's mode
        public byte targetMode; //input's target mode

        public string code; //a string that holds a group of unit sources for group unit related commands or just a task code for other commands
        //or the code of the entity component attached to the source object which will launch the command.

        public bool isSourcePrefab;
        public int sourceID; //object that launched the command
        public Vector3 sourcePosition; //initial position of the source obj

        // TargetData
        public int targetID; //target object that will get the command

        public Vector3 targetPosition; //target position.
        public Vector3 opPosition; //this field allows to add 3 extra float values in the network input struct.

        public bool playerCommand; //has this input command been requested directly by the player?+

        public IntValues intValues; //extra int attribute 
        public float floatValue; //extra float attribute

        // DEV:
        public string sourceCode;
    }
}