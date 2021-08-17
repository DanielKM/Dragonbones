using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public interface IMonoBehaviour
    {
        bool enabled { get; }

        Transform transform { get; }
        GameObject gameObject { get; }

        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);
        void StopCoroutine(IEnumerator routine);

        T GetComponent<T>();
        Component GetComponent(Type type);
        Component GetComponentInChildren(Type type);
    }
}
