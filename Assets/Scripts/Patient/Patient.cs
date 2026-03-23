using System.Collections;
using UnityEngine;

public class Patient : MonoBehaviour
{
    public PatientData data;
    public float bloodLevel;
    private float bleedMod; //1 = full bleed
    private float defaultBleedMod = 1f;
    public bool bleed; //init false
    public float currentBleedRate;

    private bool isDead = false;
    private Coroutine bleedRoutine;
    private Coroutine healRoutine;
    private Coroutine bleedReductionRoutineRef;

    void Start()
    {
        if (data != null)
        {
            Initialize(data);
        }
    }

    public void Initialize(PatientData patientData)
    {
        data = patientData;
        bloodLevel = data.startingBlood;
        currentBleedRate = data.startingBleedRate;
        defaultBleedMod = data.startingBleedMod;
        bleedMod = defaultBleedMod;
        bleed = currentBleedRate > 0f;

        if (bleed)
        {
            bleedRoutine = StartCoroutine(Bleed());
        }

        Debug.Log($"Loaded {data.patientName}: blood {bloodLevel}, stored bleed rate {currentBleedRate}");
    }

    public void applyDamage(float amt) 
    {
        if (isDead) return;

        if (bloodLevel <= 0) 
        {
            Die();
            return;
        }

        currentBleedRate += amt;
        bleed = currentBleedRate > 0f;

        if (bleedRoutine == null)
        {
            bleedRoutine = StartCoroutine(Bleed());
        }
    }

    IEnumerator Bleed() 
    {
        while (bleed && !isDead)
        {
            bloodLevel -= getBleedRate() * Time.deltaTime;
            if(bloodLevel <= 0)
            {
                Die();
                yield break;
            }
            yield return null;
        }

        bleedRoutine = null;
    }

    public void stopBleeding()
    {
        bleed = false;
        currentBleedRate = 0f;

        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
            bleedRoutine = null;
        }

        if (bleedReductionRoutineRef != null)
        {
            StopCoroutine(bleedReductionRoutineRef);
            bleedReductionRoutineRef = null;
        }

        bleedMod = defaultBleedMod;
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
            healRoutine = null;
        }

        if (dur <= 0f)
        {
            restoreVitals(totalHeal);
            return;
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
        if (bleedReductionRoutineRef != null)
        {
            StopCoroutine(bleedReductionRoutineRef);
            bleedReductionRoutineRef = null;
        }

        if (dur <= 0f)
        {
            bleedMod = defaultBleedMod;
            return;
        }

        bleedReductionRoutineRef = StartCoroutine(BleedReductionRoutine(mod, dur));

    }

    IEnumerator BleedReductionRoutine(float mod, float dur)
    {
        bleedMod = mod;
        yield return new WaitForSeconds(dur);
        bleedMod = defaultBleedMod;
        bleedReductionRoutineRef = null;
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
