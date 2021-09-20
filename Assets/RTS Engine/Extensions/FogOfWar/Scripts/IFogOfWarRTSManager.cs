using RTSEngine.Game;
using UnityEngine;

namespace RTSEngine.FoW
{
    public interface IFogOfWarRTSManager : IPostRunGameService
    {
        bool IsInFog(Vector3 position, byte minFogStrength);
    }
}
