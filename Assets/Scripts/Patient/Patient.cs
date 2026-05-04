using System.Collections;
using UnityEngine;

public class Patient : MonoBehaviour
{
    public static event System.Action<Patient> PatientDied;

    public PatientData data;
    public float bloodLevel;
    [SerializeField] private PatientWounds patientWounds;
    private float bleedMod; //1 = full bleed
    private float defaultBleedMod = 1f;
    public bool bleed; //init false
    public float currentBleedRate;
    private float directBleedRate;

    private bool isDead = false;
    private bool godMode;
    private Coroutine bleedRoutine;
    private Coroutine healRoutine;
    private Coroutine bleedReductionRoutineRef;

    void Awake()
    {
        if (patientWounds == null)
        {
            patientWounds = GetComponent<PatientWounds>();
        }

        MissionData mission = MissionFlowState.CurrentMission;
        if (mission != null && mission.patientData != null)
        {
            Initialize(mission.patientData);
        }
        else if (data != null)
        {
            Initialize(data);
        }
    }

    void Update()
    {
        RefreshBleedState();
    }

    public void Initialize(PatientData patientData)
    {
        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
            bleedRoutine = null;
        }

        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }

        if (bleedReductionRoutineRef != null)
        {
            StopCoroutine(bleedReductionRoutineRef);
            bleedReductionRoutineRef = null;
        }

        data = patientData;
        bloodLevel = data.startingBlood;
        directBleedRate = data.startingBleedRate;
        defaultBleedMod = data.startingBleedMod;
        bleedMod = defaultBleedMod;
        isDead = false;
        RefreshBleedState();

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

        directBleedRate += amt;
        RefreshBleedState();
    }

    IEnumerator Bleed() 
    {
        while (bleed && !isDead)
        {
            bloodLevel -= getBleedRate() * Time.deltaTime;
            if (godMode && bloodLevel <= 1f)
            {
                bloodLevel = 1f;
            }
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
        directBleedRate = 0f;
        RefreshBleedState();

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

    public void NotifyBleedSourcesChanged()
    {
        RefreshBleedState();
    }

    public bool IsDead => isDead;
    public bool GodMode => godMode;

    public void SetGodMode(bool enabled)
    {
        godMode = enabled;
    }

    void RefreshBleedState()
    {
        float woundBleedRate = patientWounds != null ? patientWounds.GetTotalBleedRate() : 0f;
        currentBleedRate = directBleedRate + woundBleedRate;
        bleed = currentBleedRate > 0f;

        if (isDead)
        {
            return;
        }

        if (bleed)
        {
            if (bleedRoutine == null)
            {
                bleedRoutine = StartCoroutine(Bleed());
            }
        }
        else if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
            bleedRoutine = null;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        PatientDied?.Invoke(this);
        Debug.Log("patient dead :(");
    }
}
