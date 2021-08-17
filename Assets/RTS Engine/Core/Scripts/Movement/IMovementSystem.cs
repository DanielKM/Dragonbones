using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    public interface IMovementSystem : IMonoBehaviour
    {
        bool TryGetValidPosition(Vector3 center, float radius, int areaMask, out Vector3 validPosition);
    }
}
