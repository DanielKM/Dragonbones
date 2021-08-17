using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Effect
{
    public class EffectObjectPool : MonoBehaviour, IEffectObjectPool
    {
        #region Attributes
        // Because instantiating and destroying objects is heavy on memory and since we need to show/hide effect objects multiple times in a game...
        //...this component will handle pooling those effect objects.

        // This holds all inactive effect objects of different types.
        private Dictionary<string, Queue<IEffectObject>> inactiveEffectObjs = null;
        // This holds all active effect objects of different types.
        private Dictionary<string, List<IEffectObject>> activeEffectObjs = null;

        /// <summary>
        /// Key: Code of the effect object type.
        /// </summary>
        public IReadOnlyDictionary<string, IEnumerable<IEffectObject>> ActiveEffectObjects
            => activeEffectObjs
                .ToDictionary(elem => elem.Key, elem => elem.Value.AsEnumerable());

        // Game services
        protected IGameLoggingService logger { private set; get; }

        // Other components
        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();

            inactiveEffectObjs = new Dictionary<string, Queue<IEffectObject>>();
            activeEffectObjs = new Dictionary<string, List<IEffectObject>>();
        }
        #endregion

        #region Spawning/Despawning Effect Objects
        private IEffectObject Get(IEffectObject prefab)
        {
            if(!prefab.IsValid())
            /*if (!logger.RequireTrue(prefab.IsValid(),
                $"[{GetType().Name}] Unable to retrieve a valid effect object for an invalid prefab!",
                source: this,
                type: LoggingType.warning))*/
                return null;

            if (inactiveEffectObjs.TryGetValue(prefab.Code, out Queue<IEffectObject> currentQueue) == false) //if the queue for this effect object type is not found
            {
                currentQueue = new Queue<IEffectObject>();
                inactiveEffectObjs.Add(prefab.Code, currentQueue); //add it
            }

            if (currentQueue.Count == 0) //if the queue is empty then we need to create a new effect object of this types
            {
                //create new effect object, init it and add it to the queue
                IEffectObject newEffect = GameObject.Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<IEffectObject>();
                newEffect.Init(gameMgr);
                currentQueue.Enqueue(newEffect);
            }
            return currentQueue.Dequeue(); //return the first inactive effect object in this queue
        }

        public IEffectObject Spawn(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation = default, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0.0f, bool isUIElement = false)
            => prefab.IsValid()
            ? Spawn(prefab.GetComponent<IEffectObject>(), spawnPosition, spawnRotation, parent, enableLifeTime, autoLifeTime, lifeTime, isUIElement)
            : null;

        //a method that spawns an effect object considering a couple of options
        public IEffectObject Spawn(IEffectObject prefab, Vector3 spawnPosition, Quaternion spawnRotation = default, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0.0f, bool isUIElement = false)
        {
            //get the attack effect (either create it or get one tht is inactive):
            IEffectObject newEffect = Get(prefab);

            if(!newEffect.IsValid())
            /*if (!logger.RequireValid(newEffect,
                $"[{GetType().Name}] Unable to find or create inactive effect object for prefab: {prefab}", 
                source: this,
                type: LoggingType.warning))*/
                return null;

            //make the object child of the assigned parent transform
            newEffect.transform.SetParent(parent, true);

            if (isUIElement)
            {
                newEffect.GetComponent<RectTransform>().localPosition = spawnPosition;
                newEffect.GetComponent<RectTransform>().localRotation = spawnRotation;
            }
            else
            {
                //set the effect's position and rotation
                newEffect.transform.position = spawnPosition;
                newEffect.transform.rotation = spawnRotation;
            }

            // Add the new effect object to the active effect objects lists:
            if (!activeEffectObjs.TryGetValue(newEffect.Code, out var currActiveList))
            {
                currActiveList = new List<IEffectObject>();
                activeEffectObjs.Add(newEffect.Code, currActiveList);
            }
            currActiveList.Add(newEffect);

            newEffect.Activate(enableLifeTime, autoLifeTime, lifeTime);

            return newEffect;
        }

        public void Despawn(IEffectObject instance, bool destroyed = false)
        {
            if (activeEffectObjs.TryGetValue(instance.Code, out var currActiveList))
                currActiveList.Remove(instance);

            // If the effect object has been destroyed then it can not be used anymore so no need to put it back in the inactive queues
            if (destroyed)
                return;

            // If the queue for this effect object type is not found, add a new entry to the dictionary
            if (!inactiveEffectObjs.TryGetValue(instance.Code, out var currInactiveQueue))
            {
                currInactiveQueue = new Queue<IEffectObject>();
                inactiveEffectObjs.Add(instance.Code, currInactiveQueue);
            }
            currInactiveQueue.Enqueue(instance);
        }
        #endregion
    }
}