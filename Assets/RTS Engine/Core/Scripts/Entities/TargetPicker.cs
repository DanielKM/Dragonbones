using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* TargetPicker script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.Entities
{
    /// <summary>
    /// Defines the available types to pick a target from.
    /// all: all targets
    /// allInList: all targets defined in the list
    /// allButInList: all targets but the ones defined in the list
    /// </summary>
    public enum TargetPickerType {all, allInList, allButInList}

    /// <summary>
    /// Generic data type that allows to pick all types of a target T, just a list of elements of type T or everything but a list of elements of type T
    /// </summary>
    /// <typeparam name="T">Type of the target.</typeparam>
    /// <typeparam name="V">Type of the list that defines the possible (or not) targets.</typeparam>
    [System.Serializable]
    public abstract class TargetPicker<T, V>
    {
        [SerializeField, Tooltip("How to handle the list of potential targets?")]
        protected TargetPickerType type = TargetPickerType.all;
        [SerializeField, Tooltip("Defines the potential targets.")]
        protected V options;

        /// <summary>
        /// Determines whether a target 't' can be picked as a valid target.
        /// </summary>
        /// <param name="target">The target to test its validity.</param>
        /// <returns>true if the target 't' can be picked, otherwise false.</returns>
        public virtual bool IsValidTarget (T target)
        {
            return (type == TargetPickerType.all
                || (type == TargetPickerType.allInList && IsInList(target))
                || (type == TargetPickerType.allButInList && !IsInList(target)));
        }

        /// <summary>
        /// Is the target 't' in the list?
        /// </summary>
        /// <param name="target">Target instance to test</param>
        /// <returns>True if the target 't' is in the list, otherwise false.</returns>
        protected abstract bool IsInList(T target);
    }
}
