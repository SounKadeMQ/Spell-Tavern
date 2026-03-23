using TMPro;
using UnityEngine;
//AccuracyCheck.cs

/*
0.00 - 1.60 = NICE!!!
1.61 - 1.8235 = GREAT!!
1.8236 - 2.05 = GOOD!
2.06 - 3.00 = EH...!
3.00+ = MISS!

effectMultiplier = 1 / 1 (1 + accuracy);

Perfect; waterHeal = 100 * 0.3 * 1.1 = 33
Accurate; waterHeal = 100 * 0.3 * 0.75 = 22.5
Good; waterHeal = 100 * 0.3 * 0.55 = 16.5
Eh; waterHeal = 100 * 0.3 * 0.4 = 12
MISS; waterHeal = 100 * 0.3 * 0.1 = 3
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
    [SerializeField] private bool requireSpellSelection = true;
    [SerializeField] private SpellType selectedSpell = SpellType.None;

    [Header("Spell Select")]
    [SerializeField] private KeyCode airSpellKey = KeyCode.W;
    [SerializeField] private KeyCode fireSpellKey = KeyCode.A;
    [SerializeField] private KeyCode earthSpellKey = KeyCode.S;
    [SerializeField] private KeyCode waterSpellKey = KeyCode.D;
    [SerializeField] private GameObject waterRuneVisual;
    [SerializeField] private GameObject earthRuneVisual;
    [SerializeField] private GameObject fireRuneVisual;
    [SerializeField] private Color startIndicatorColor = new Color(0.7f, 0.95f, 1f, 0.9f);

    private LineRenderer waterRuneLine;
    private Vector3[] waterRuneTemplateOffsets;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI spellJudgementText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int score;

    [Header("Accuracy Tuning")]
    [SerializeField] private float niceThreshold = 1.6f;
    [SerializeField] private float greatThreshold = 1.8235f;
    [SerializeField] private float goodThreshold = 2.05f;
    [SerializeField] private float ehThreshold = 3f;

    [Header("Speed Tuning")]
    [SerializeField] private float idealCastTime = 0.85f;
    [SerializeField] private float maxCastTime = 2.2f;
    [SerializeField] private float speedAccuracyBonus = 0.35f;
    [SerializeField] private float speedAccuracyPenalty = 0.2f;
    [SerializeField] private float quickCastThreshold = 0.95f;

    [Header("Cast Popup")]
    [SerializeField] private float popupHeightOffset = 1.25f;
    [SerializeField] private Color popupColor = new Color(1f, 0.95f, 0.55f);

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

    void Start()
    {
        if (waterRuneVisual == null && waterAccuracyCheck != null)
        {
            waterRuneVisual = waterAccuracyCheck.gameObject;
        }

        if (waterAccuracyCheck != null)
        {
            waterRuneLine = waterAccuracyCheck.GetComponent<LineRenderer>();
            CacheWaterRuneTemplate();
        }

        if (requireSpellSelection)
        {
            selectedSpell = SpellType.None;
        }

        UpdateRuneVisibility();
    }

    void Update()
    //todo: implement later
    {
        if (patient != null && patientWounds == null)
        {
            patientWounds = patient.GetComponent<PatientWounds>();
        }

        HandleSpellSelectionInput();

        if (selectedSpell == SpellType.Water &&
            mouseDraw != null &&
            mouseDraw.TryConsumeStrokeStart(out Vector3 strokeStartPosition))
        {
            MoveWaterRuneTo(strokeStartPosition);
            StrokeStartIndicator.Create(strokeStartPosition, startIndicatorColor);
        }

        if (useDrawnWaterSpell &&
            selectedSpell == SpellType.Water &&
            waterAccuracyCheck != null &&
            waterAccuracyCheck.TryConsumeAccuracy(out float drawnAccuracy))
        {
            float finalAccuracy = GetSpeedAdjustedAccuracy(drawnAccuracy);
            bool isQuickCast = IsQuickCast();
            Vector3 popupWorldPosition = GetPopupWorldPosition();
            CastSelectedSpell(finalAccuracy, isQuickCast, popupWorldPosition);
        }

        if (!enableDebugHotkeys)
        {
            return;
        }

        if (requireSpellSelection && selectedSpell == SpellType.None)
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
            CastWater(0.38f); // great
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CastWater(2.158401f); // good
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            CastWater(5.5f); // miss
        }
    }

    public void CastWater(float acc)
    {
        CastWater(acc, false, false, Vector3.zero);
    }

    void CastWater(float acc, bool showPopup, bool isQuickCast, Vector3 popupWorldPosition)
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
        if (showPopup)
        {
            ShowCastPopup(judgement, isQuickCast, popupWorldPosition);
        }


        if(audioSource != null && waterSFX != null)
        {
            audioSource.PlayOneShot(waterSFX);
        }

        Debug.Log(
            "water cast - acc: " + acc.ToString("F3") +
            " - cast time: " + (mouseDraw != null ? mouseDraw.LastStrokeDuration.ToString("F2") : "n/a") +
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
        UpdateRuneVisibility();
    }

    public SpellType GetSelectedSpell()
    {
        return selectedSpell;
    }

    void CastSelectedSpell(float acc)
    {
        CastSelectedSpell(acc, false, Vector3.zero);
    }

    void CastSelectedSpell(float acc, bool isQuickCast, Vector3 popupWorldPosition)
    {
        if (selectedSpell == SpellType.None)
        {
            return;
        }

        switch (selectedSpell)
        {
            case SpellType.Water:
                CastWater(acc, true, isQuickCast, popupWorldPosition);
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
        if (acc <= niceThreshold)
        {
            return SpellJudgement.Nice;
        }

        if (acc <= greatThreshold)
        {
            return SpellJudgement.Great;
        }

        if (acc <= goodThreshold)
        {
            return SpellJudgement.Good;
        }

        if (acc <= ehThreshold)
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
        if (acc <= niceThreshold)
        {
            return 1.1f;
        }

        if (acc <= greatThreshold)
        {
            return 0.85f;
        }

        if (acc <= goodThreshold)
        {
            return 0.7f;
        }

        if (acc <= ehThreshold)
        {
            return 0.5f;
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

        ShowCastPopup(judgement, IsQuickCast(), GetPopupWorldPosition());

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

    float GetSpeedAdjustedAccuracy(float baseAccuracy)
    {
        if (mouseDraw == null)
        {
            return baseAccuracy;
        }

        float strokeDuration = mouseDraw.LastStrokeDuration;
        if (strokeDuration <= 0f)
        {
            return baseAccuracy;
        }

        float quickness = Mathf.InverseLerp(maxCastTime, idealCastTime, strokeDuration);
        float adjustedAccuracy = baseAccuracy - (speedAccuracyBonus * quickness);

        if (strokeDuration > maxCastTime)
        {
            adjustedAccuracy += (strokeDuration - maxCastTime) * speedAccuracyPenalty;
        }

        return Mathf.Max(0f, adjustedAccuracy);
    }

    bool IsQuickCast()
    {
        return mouseDraw != null &&
               mouseDraw.LastStrokeDuration > 0f &&
               mouseDraw.LastStrokeDuration <= quickCastThreshold;
    }

    Vector3 GetPopupWorldPosition()
    {
        if (mouseDraw == null)
        {
            return transform.position + (Vector3.up * popupHeightOffset);
        }

        return mouseDraw.LastStrokeEndWorldPosition + (Vector3.up * popupHeightOffset);
    }

    void ShowCastPopup(SpellJudgement judgement, bool isQuickCast, Vector3 popupWorldPosition)
    {
        string popupText = isQuickCast
            ? "QUICK " + GetPopupText(judgement)
            : GetPopupText(judgement);

        SpellCastPopup.Create(popupWorldPosition, popupText, popupColor);
    }

    string GetPopupText(SpellJudgement judgement)
    {
        switch (judgement)
        {
            case SpellJudgement.Nice:
                return "NICE!!!";
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

    void HandleSpellSelectionInput()
    {
        if (Input.GetKeyDown(airSpellKey))
        {
            SetSelectedSpell(SpellType.Wind);
        }
        else if (Input.GetKeyDown(fireSpellKey))
        {
            SetSelectedSpell(SpellType.Fire);
        }
        else if (Input.GetKeyDown(earthSpellKey))
        {
            SetSelectedSpell(SpellType.Earth);
        }
        else if (Input.GetKeyDown(waterSpellKey))
        {
            SetSelectedSpell(SpellType.Water);
        }
    }

    void UpdateRuneVisibility()
    {
        SetRuneVisible(waterRuneVisual, selectedSpell == SpellType.Water);
        SetRuneVisible(earthRuneVisual, selectedSpell == SpellType.Earth);
        SetRuneVisible(fireRuneVisual, selectedSpell == SpellType.Fire);
    }

    void SetRuneVisible(GameObject runeVisual, bool visible)
    {
        if (runeVisual != null)
        {
            runeVisual.SetActive(visible);
        }
    }

    void CacheWaterRuneTemplate()
    {
        if (waterRuneLine == null || waterRuneLine.positionCount == 0)
        {
            waterRuneTemplateOffsets = null;
            return;
        }

        waterRuneTemplateOffsets = new Vector3[waterRuneLine.positionCount];
        Vector3 anchor = waterRuneLine.GetPosition(0);

        for (int i = 0; i < waterRuneLine.positionCount; i++)
        {
            waterRuneTemplateOffsets[i] = waterRuneLine.GetPosition(i) - anchor;
        }
    }

    void MoveWaterRuneTo(Vector3 anchorPosition)
    {
        if (waterRuneLine == null || waterRuneTemplateOffsets == null)
        {
            return;
        }

        for (int i = 0; i < waterRuneTemplateOffsets.Length; i++)
        {
            waterRuneLine.SetPosition(i, anchorPosition + waterRuneTemplateOffsets[i]);
        }
    }
}
