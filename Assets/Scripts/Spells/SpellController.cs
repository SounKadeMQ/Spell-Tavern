using TMPro;
using UnityEngine;
//AccuracyCheck.cs

/*
0.00 - 0.35 = perfect
0.36 - 2.15 = very accurate
2.16 - 5.00 = poor
5.00+ = are you trying?

effectMultiplier = 1 / 1 (1 + accuracy);

Perfect; waterHeal = 100 * 0.3 * 1.1 = 22
Accurate; waterHeal = 100 * 0.3 * 0.75 = 15
Poor; waterHeal = 100 * 0.3 * 0.4 = 10
MISS; waterHeal = 100 * 0.3 * 0.1 = 2
*/

public class SpellController : MonoBehaviour
{
    public enum SpellJudgement
    {
        Nice,
        Great,
        Good,
        Eh,
        Miss
    }

    public enum SpellType
    {
        None,
        Water,
        Wind,
        Earth,
        Lightning,
        Fire
    }

    public Patient patient;
    [SerializeField] private PatientWounds patientWounds;
    [SerializeField] private MouseDraw mouseDraw;
    public AccuracyCheck waterAccuracyCheck;

    public bool readyToMerge;
    [SerializeField] private bool useDrawnWaterSpell = true;
    [SerializeField] private bool enableDebugHotkeys = true;
    [SerializeField] private SpellType selectedSpell = SpellType.Water;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI spellJudgementText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int score;

    [Header("Water Spell")]
    public float waterPotency = 100f;
    public float waterDuration = 2f;
    public float testAccuracy = 0.38f;

    [Header("Wind Spell")]
    public float windPotency = 100f;
    public float windDuration = 2f;
    public float windTestAccuracy = 0.38f;

    [Header("Earth Spell")]
    public float earthPotency = 100f;
    public float earthDuration = 2f;
    public float earthTestAccuracy = 0.38f;

    [Header("Lightning Spell")]
    public float lightningPotency = 100f;
    public float lightningDuration = 2f;
    public float lightningTestAccuracy = 0.38f;

    [Header("Fire Spell")]
    public float firePotency = 100f;
    public float fireDuration = 2f;
    public float fireTestAccuracy = 0.38f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip waterSFX;

    void Update()
    //todo: implement later
    {
        if (patient != null && patientWounds == null)
        {
            patientWounds = patient.GetComponent<PatientWounds>();
        }

        if (useDrawnWaterSpell &&
            waterAccuracyCheck != null &&
            waterAccuracyCheck.TryConsumeAccuracy(out float drawnAccuracy))
        {
            CastSelectedSpell(drawnAccuracy);
        }

        if (!enableDebugHotkeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CastSelectedSpell(GetSelectedSpellTestAccuracy());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CastEarth(earthTestAccuracy);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            CastFire(fireTestAccuracy);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CastWater(0.20f); // perfect
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            CastWater(0.38f); // v acc
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CastWater(2.158401f); // poor
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            CastWater(5.5f); // are you trying
        }
    }

    public void CastWater(float acc)
    {
        if (patient == null) return;

        SpellJudgement judgement = GetSpellJudgement(acc);
        float mult = GetAccuracyMultiplier(acc);
        float totalHeal = calcWaterHeal(waterPotency, mult);
        int pointsAwarded = GetPointsForJudgement(judgement);

        float t = Mathf.InverseLerp(0.1f, 1.1f, mult);
        float reduction = Mathf.Lerp(0.6f, 0.3f, t);

        patient.startHoT(totalHeal, waterDuration);
        patient.bleedReduction(reduction, waterDuration); //reduces bleed based on accuracy of the spell
        score += pointsAwarded;

        UpdateFeedbackUI(judgement);


        if(audioSource != null && waterSFX != null)
        {
            audioSource.PlayOneShot(waterSFX);
        }

        Debug.Log(
            "water cast - acc: " + acc.ToString("F3") +
            " - judgement: " + GetJudgementText(judgement) +
            " - points: " + pointsAwarded +
            " - multiplier: " + mult.ToString("F2") +
            " - total heal: " + totalHeal.ToString("F2") +
            " - bleed reduction: " + reduction.ToString("F2") 
        );
    }

    public void CastEarth(float acc)
    {
        CastTargetedSpell(SpellType.Earth, acc);
    }

    public void CastFire(float acc)
    {
        CastTargetedSpell(SpellType.Fire, acc);
    }

    public void SetSelectedSpell(SpellType spell)
    {
        selectedSpell = spell;
    }

    public SpellType GetSelectedSpell()
    {
        return selectedSpell;
    }

    void CastSelectedSpell(float acc)
    {
        switch (selectedSpell)
        {
            case SpellType.Water:
                CastWater(acc);
                break;
            case SpellType.Earth:
                CastEarth(acc);
                break;
            case SpellType.Fire:
                CastFire(acc);
                break;
        }
    }

    SpellJudgement GetSpellJudgement(float acc)
    {
        if (acc <= 0.35f)
        {
            return SpellJudgement.Nice;
        }

        if (acc <= 2.15f)
        {
            return SpellJudgement.Great;
        }

        if (acc <= 3.5f)
        {
            return SpellJudgement.Good;
        }

        if (acc <= 5f)
        {
            return SpellJudgement.Eh;
        }

        return SpellJudgement.Miss;
    }

    int GetPointsForJudgement(SpellJudgement judgement)
    {
        switch (judgement)
        {
            case SpellJudgement.Nice:
                return 300;
            case SpellJudgement.Great:
                return 250;
            case SpellJudgement.Good:
                return 150;
            case SpellJudgement.Eh:
                return 50;
            default:
                return 0;
        }
    }

    string GetJudgementText(SpellJudgement judgement)
    {
        switch (judgement)
        {
            case SpellJudgement.Nice:
                return "NICE!!! (300 points)";
            case SpellJudgement.Great:
                return "GREAT!!";
            case SpellJudgement.Good:
                return "GOOD!";
            case SpellJudgement.Eh:
                return "EH...!";
            default:
                return "MISS!";
        }
    }

    void UpdateFeedbackUI(SpellJudgement judgement)
    {
        string feedback = GetJudgementText(judgement);

        if (spellJudgementText != null)
        {
            spellJudgementText.text = feedback;
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    float GetAccuracyMultiplier(float acc)
    {
        if (acc <= 0.35f)
        {
            return 1.1f;
        }

        if (acc <= 2.15f)
        {
            return 0.75f;
        }

        if (acc <= 3.5f)
        {
            return 0.55f;
        }

        if (acc <= 5f)
        {
            return 0.4f;
        }

        return 0.1f;
    }

    float calcWaterHeal(float pot, float mult)
    {
        return pot * 0.3f * mult;
    }

    void CastTargetedSpell(SpellType spellType, float acc)
    {
        if (!TryGetTargetWound(out CutWound wound))
        {
            if (spellJudgementText != null)
            {
                spellJudgementText.text = "No wound targeted.";
            }

            Debug.Log(spellType + " cast failed: no wound targeted.");
            return;
        }

        SpellJudgement judgement = GetSpellJudgement(acc);
        int pointsAwarded = GetPointsForJudgement(judgement);
        bool treated = wound.TryApplySpell(spellType, out string outcome);

        if (treated)
        {
            score += pointsAwarded;
        }

        if (spellJudgementText != null)
        {
            spellJudgementText.text = outcome;
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        Debug.Log(
            spellType + " cast - acc: " + acc.ToString("F3") +
            " - outcome: " + outcome +
            " - points: " + (treated ? pointsAwarded : 0));
    }

    bool TryGetTargetWound(out CutWound wound)
    {
        wound = null;

        if (patientWounds == null)
        {
            return false;
        }

        if (mouseDraw != null &&
            mouseDraw.CurrentLine != null &&
            patientWounds.TryGetWoundTouchedByLine(mouseDraw.CurrentLine, out wound))
        {
            return true;
        }

        return patientWounds.TryGetFirstOpenWound(out wound);
    }

    float GetSelectedSpellTestAccuracy()
    {
        switch (selectedSpell)
        {
            case SpellType.Earth:
                return earthTestAccuracy;
            case SpellType.Fire:
                return fireTestAccuracy;
            default:
                return testAccuracy;
        }
    }
}
