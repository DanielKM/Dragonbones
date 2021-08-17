using UnityEngine;

namespace RTSEngine.Utilities
{
    /// <summary>
    /// Defines the types of fetching an element from a set where:
    /// random: One item is randomly chosen each time.
    /// randomNoRep: One item is randomly chosen each time with the guarantee that the same item will not be chosen consecutively.
    /// inOrder: Fetch items in the order they were defined in.
    /// </summary>
    public enum FetchType { random, randomNoRep, inOrder }

    [System.Serializable]
    public abstract class Fetcher<T> where T : Object
    {
        #region Class Attributes
        [SerializeField, Tooltip("How would the item be fetched each time?")]
        private FetchType fetchType = FetchType.random;

        [SerializeField, Tooltip("An array of items that can be potentially fetched.")]
        private T[] items = new T[0];

        public int Count
        {
            get
            {
                return items.Length;
            }
        }

        private int cursor = 0; //for the inOrder and randomNoRep fetch types, this is used to track the last fetched items.
        #endregion

        #region Fetching
        private T GetNext ()
        {
            //move the cursor one step further through the array
            if (cursor >= items.Length - 1)
                cursor = 0;
            else
                cursor++;

            return items[cursor]; //return the next item in the array
        }

        public virtual T Fetch ()
        {
            if (!CanFetch() 
                || items.Length <= 0)
                return null;

            OnPreFetch();

            switch (fetchType)
            {
                case FetchType.randomNoRep:

                    int itemIndex = Random.Range(0, items.Length); 

                    if (itemIndex == cursor) 
                        return GetNext(); 
                    else 
                    {
                        cursor = itemIndex; 
                        return items[cursor];
                    }

                case FetchType.inOrder: 
                    return GetNext();

                default: 
                    return items[Random.Range(0, items.Length)];
            }
        }

        protected virtual void OnPreFetch() { }

        protected virtual bool CanFetch() => true;
        #endregion
    }
}
