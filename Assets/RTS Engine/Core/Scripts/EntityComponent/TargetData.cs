using RTSEngine.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public struct TargetData<T> where T : IEntity
    {
        public Vector3 position;
        public T instance;

        public Vector3 opPosition;

        public static implicit operator TargetData<T>(T instance) => RTSHelper.ToTargetData<T>(instance);
        public static implicit operator TargetData<T>(Vector3 position) => new TargetData<T> { position = position };

        public static implicit operator TargetData<IEntity>(TargetData<T> data) => new TargetData<IEntity> { instance = data.instance, position = data.position, opPosition = data.opPosition };
        public static implicit operator TargetData<T>(TargetData<IEntity> data)
        {
            return new TargetData<T>
            {
                instance = (data.instance is T) ? (T)data.instance : default,
                position = data.position,
                opPosition = data.opPosition
            };

        }
    }
}
