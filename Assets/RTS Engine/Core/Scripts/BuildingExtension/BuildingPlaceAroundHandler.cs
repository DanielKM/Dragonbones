using System.Collections.Generic;
using System.Linq;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Search;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlaceAroundHandler
    {
        protected IBuilding building { private set; get; }

        private Stack<BuildingPlaceAroundData> nextDataStack = null; 
        public BuildingPlaceAroundData CurrData { get; private set; }

        // Game services
        protected IGridSearchHandler gridSearch { private set; get; }

        public BuildingPlaceAroundHandler(IGameManager gameMgr, IBuilding building, BuildingPlaceAroundData data)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            this.building = building;

            this.CurrData = data;
            nextDataStack = new Stack<BuildingPlaceAroundData>();
        }

        public BuildingPlaceAroundHandler(IGameManager gameMgr, IBuilding building, IEnumerable<BuildingPlaceAroundData> possibleData)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>(); 

            this.building = building;

            nextDataStack = new Stack<BuildingPlaceAroundData>(possibleData.Reverse());
        }

        public bool TrySetNextData()
        {
            if(nextDataStack.Count > 0)
            {
                CurrData = nextDataStack.Pop();
                return true;
            }

            return false;
        }

        public bool IsPlaceAroundValid()
        {
            return gridSearch.Search(
                building.transform.position,
                CurrData.range.max,
                CurrData.IsValidType,
                playerCommand: false,
                out IEntity _,
                findClosest: false) == ErrorMessage.none;
        }
    }
}

