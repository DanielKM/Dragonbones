using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Effect
{
    public interface IEffectObject : IMonoBehaviour
    {
        string Code { get; }
        EffectObjectState State { get; }

        AudioSource AudioSourceComponent { get; }

        void Init(IGameManager gameMgr);

        void Activate(bool enableLifeTime, bool useDefaultLifeTime = true, float customLifeTime = 0);
        void Deactivate(bool useDisableTime = true);
    }
}
