using System.Linq;

using UnityEngine;

namespace RTSEngine.Entities
{
    [System.Serializable]
    public class EntityAmountHandler
    {
        [SerializeField]
        private int defaultAmount = 1;

        [SerializeField]
        private EntityAmount[] amounts = new EntityAmount[0];

        public int GetAmount(IEntity entity)
        {
            EntityAmount customAmount = amounts
                .Where(entityAmount => entityAmount.entities.Contains(entity))
                .FirstOrDefault();

            return customAmount != null ? customAmount.amount : defaultAmount;
        }
    }
}
