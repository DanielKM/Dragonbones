using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine.Movement
{
    public class NavmeshMovementSystem : MonoBehaviour, IMovementSystem
    {
        public bool TryGetValidPosition(Vector3 center, float radius, int areaMask, out Vector3 validPosition)
        {
            if (NavMesh.SamplePosition(center, out NavMeshHit hit, radius, areaMask))
            {
                validPosition = hit.position;
                return true;
            }

            validPosition = center;
            return false;
        }
    }
}
