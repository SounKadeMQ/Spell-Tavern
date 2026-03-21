using System.Collections;
using NUnit.Framework;
using Unity.IntegerTime;
using Unity.VisualScripting;
using UnityEditor.U2D.Tooling.Analyzer;
using UnityEngine;

public class Patient : MonoBehaviour
{
    public float bloodLevel = 100f;
    private float bleedMod = 1f; //1 = full bleed
    public bool bleed; //init false
    public float currentBleedRate = 0f;

    private bool isDead = false;
    private Coroutine bleedRoutine;
    private Coroutine healRoutine;

    public void applyDamage(float amt) 
    {
        if (isDead) return;

        if (bloodLevel <= 0) 
        {
            Die();
        }

        bleed = true;
        currentBleedRate = amt;
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
            currentBleedRate = 0f;
        }
    }

    public void restoreVitals(float amt)
    {
        if (isDead) return;
        bloodLevel += amt;
        if(bloodLevel > 100f)
        {
            bloodLevel = 100f;
        }
    }

    public void startHoT(float totalHeal, float dur)
    {
        if (isDead) return;
        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
        }
        healRoutine = StartCoroutine(HealOverTime(totalHeal, dur));
    }

    IEnumerator HealOverTime(float totalHeal, float duration)
    {
        float elapsed = 0f;
        float healPerSecond = totalHeal / duration;

        while (elapsed < duration && !isDead)
        {
            restoreVitals(healPerSecond * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        healRoutine = null;
    }

    public void setBleedModifier(float mod)
    {
        bleedMod = mod;
    }

    public void bleedReduction(float mod, float dur)
    {
        StartCoroutine(bleedReductionRoutine(mod, dur));

    }

    IEnumerator bleedReductionRoutine(float mod, float dur)
    {
        bleedMod = mod;
        yield return new WaitForSeconds(dur);
        bleedMod = 1f;
    }
 
    public float getBleedRate()
    {
        return currentBleedRate * bleedMod;
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("patient dead :(");
    }
}
