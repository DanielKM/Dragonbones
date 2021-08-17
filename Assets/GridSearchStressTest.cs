using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Search;

namespace RTSEngine.Tests.Stress
{
    public class GridSearchStressTest : MonoBehaviour, IPostRunGameService
    {
        [SerializeField]
        private IntRange size = new IntRange(800, 1000);

        [SerializeField]
        private FloatRange period = new FloatRange(1.0f, 2.0f);
        private TimeModifiedTimer timer;

        [SerializeField]
        private FloatRange range = new FloatRange(15.0f, 30.0f);

        [SerializeField]
        private Vector3 minPosition = Vector3.zero;
        [SerializeField]
        private Vector3 maxPosition = Vector3.zero;

        [SerializeField]
        private IntRange targetValidConditionCount = new IntRange(1, 5);

        protected IGridSearchHandler gridSearch { private set; get; }

        public void Init(IGameManager gameMgr)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            timer = new TimeModifiedTimer(period);
        }

        private void Update()
        {
            if (!timer.ModifiedDecrease())
                return;

            timer.Reload(period);

            int count = 0;

            for (int i = 0; i < size.RandomValue; i++)
            {
                gridSearch.Search(
                    new Vector3(Random.Range(minPosition.x, maxPosition.x), Random.Range(minPosition.y, maxPosition.y), Random.Range(minPosition.z, maxPosition.z)),
                    range.RandomValue,
                    IsTargetValid,
                    true,
                    out IEntity target,
                    false
                    );

                count++;
            }

            print("did this much: " + count);
        }

        private ErrorMessage IsTargetValid(TargetData<IEntity> target, bool playerCommand)
        {
            int successConditions = targetValidConditionCount.RandomValue;

            while(successConditions > 0)
            {
                if (true)
                    successConditions--;
            }

            return ErrorMessage.none;
        }
    }
}
