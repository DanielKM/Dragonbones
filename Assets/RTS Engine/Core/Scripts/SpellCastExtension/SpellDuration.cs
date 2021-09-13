using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellDuration : MonoBehaviour
{
    [SerializeField, Tooltip("Defines the duration that the spell is supposed to persist for in seconds")]
    public int secondsToDestroy;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DestroyThisSpell(secondsToDestroy));
    }

    private IEnumerator DestroyThisSpell(int seconds)
    {
        yield return new WaitForSeconds(seconds);

        Destroy(gameObject);
    }
}