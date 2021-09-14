using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Attack;

public class SpellDuration : MonoBehaviour
{
    [SerializeField, Tooltip("Defines if the target should be destroyed after the number of seconds defined by Duration")]
    public bool destroy;

    [SerializeField, Tooltip("Defines the duration that the spell is supposed to persist for in seconds")]
    public int secondsToDestroy;
            
    // Start is called before the first frame update
    void Start()
    {
        if(!destroy)
        {
            StartCoroutine(DestroyThisSpell(secondsToDestroy));
        }
    }
    
    private IEnumerator DestroyThisSpell(int seconds)
    {
        yield return new WaitForSeconds(seconds);

        Destroy(gameObject);
    }
}