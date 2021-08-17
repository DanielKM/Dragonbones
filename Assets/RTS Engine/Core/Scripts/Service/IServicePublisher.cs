using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Service
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">All services published must implement this type.</typeparam>
    public interface IServicePublisher<T>
    {
        E GetService<E>() where E : T;
    }
}
