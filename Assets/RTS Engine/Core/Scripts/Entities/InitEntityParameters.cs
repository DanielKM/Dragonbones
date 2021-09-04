using RTSEngine.BuildingExtension;
using RTSEngine.EntityComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Entities
{
    public abstract class InitEntityParameters
    {
        public int factionID;
        public bool free;

        public bool setInitialHealth;
        public int initialHealth;

        public bool playerCommand;
    }

    public class InitBuildingParameters : InitEntityParameters
    {
        public IBorder buildingCenter;
    }

    public class InitUnitParameters : InitEntityParameters
    {
        public IRallypoint rallypoint;
        public IEntityComponent creatorEntityComponent;

        public Vector3 gotoPosition;
    }

    public class InitUnitParametersInput : InitEntityParameters 
    {
        public string rallypointCode;
        public string creatorEntityComponentCode;

        public Vector3 gotoPosition;
    }

    public class InitResourceParameters : InitEntityParameters
    {

    }

    public class InitSpellParameters : InitEntityParameters
    {

    }
}
