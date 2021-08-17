using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Effect
{
    public interface IEffectObjectPool : IPreRunGameService
    {
        IReadOnlyDictionary<string, IEnumerable<IEffectObject>> ActiveEffectObjects { get; }

        IEffectObject Spawn(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation = default, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0, bool isUIElement = false);
        IEffectObject Spawn(IEffectObject prefab, Vector3 spawnPosition, Quaternion spawnRotation = default, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0, bool isUIElement = false);

        void Despawn(IEffectObject instance, bool destroyed = false);
    }
}