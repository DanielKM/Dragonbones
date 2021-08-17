using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Service
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Type of the service publisher (for game only services, this would be the IGameManager)</typeparam>
    public interface IServiceComponent<T>
    {
        void Init(T manager);
    }
}
