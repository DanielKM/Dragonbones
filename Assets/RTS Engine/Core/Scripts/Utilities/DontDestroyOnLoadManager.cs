using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Utilities
{
    public static class DontDestroyOnLoadManager
    {
        private static List<GameObject> ddolObjects = new List<GameObject>();
        public static IEnumerable<GameObject> AllDdolObjects
        {
            get
            {
                ddolObjects = ddolObjects.Where(ddolObjects => ddolObjects.IsValid()).ToList();
                return ddolObjects;
            }
        }

        public static void DontDestroyOnLoad(this GameObject obj)
        {
            UnityEngine.Object.DontDestroyOnLoad(obj);
            ddolObjects.Add(obj);
        }

        internal static void Destroy(GameObject obj)
        {
            ddolObjects.Remove(obj);
            GameObject.Destroy(obj);
        }
    }
}
