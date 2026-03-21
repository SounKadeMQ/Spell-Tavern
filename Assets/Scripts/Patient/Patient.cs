using System.Collections;
using Unity.IntegerTime;
using Unity.VisualScripting;
using UnityEngine;

public class Patient : MonoBehaviour
{
    public float bloodLevel = 100f;
    public bool bleed; //init false

    private bool isDead = false;
    private Coroutine bleedRoutine;

    public void applyDamage(float amt) 
    {
        if (isDead) return;

        if (bloodLevel <= 0) 
        {
            Die();
        }

        bleed = true;
        bleedRoutine = StartCoroutine(Bleed(amt)); //sits here until used
    }

    IEnumerator Bleed(float rate) 
    {
        while (bleed && !isDead)
        {
            bloodLevel -= rate * Time.deltaTime;
            if(bloodLevel <= 0)
            {
                Die();
                yield break;
            }
            yield return null;
        }
    }

    public void stopBleeding()
    {
        bleed = false;
        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
            bleedRoutine = null;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("patient dead :(");
    }
}
