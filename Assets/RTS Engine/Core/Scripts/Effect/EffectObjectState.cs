using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Effect
{
    /// <summary>
    /// Represents the state of an effect object.
    /// inactive: disabled effect object which can be retrieved from the pool to be used.
    /// running: enabled effect object that can not be retrieved from the pool.
    /// disabling: effect object in transition from being running to inactive. In this state, the effect object can not be retrieved from the pool.
    /// </summary>
    public enum EffectObjectState { inactive, running, disabling };
}
