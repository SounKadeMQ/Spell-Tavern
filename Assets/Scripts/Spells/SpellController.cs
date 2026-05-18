using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//AccuracyCheck.cs

/*
0.05 or less = PERFECT!!!!
0.07 = GREAT!!!
0.09 = GOOD!!
0.12 = EH...!
0.12+ = MISS

effectMultiplier = 1 / 1 (1 + accuracy);

Perfect; waterHeal = 100 * 0.3 * 1.1 = 33
Great; waterHeal = 100 * 0.3 * 0.85 = 25.5
Good; waterHeal = 100 * 0.3 * 0.7 = 21
Eh; waterHeal = 100 * 0.3 * 0.5 = 15
Miss; waterHeal = 100 * 0.3 * 0.1 = 3
*/

public class SpellController : MonoBehaviour
{
    public static event System.Action<SpellType> SpellCastSucceeded;
    public static event System.Action<SpellType> SpellSelected;

    [System.Serializable]
    public class SpellAccuracyThresholds
    {
        public float perfectThreshold = 0.05f;
        public float greatThreshold = 0.07f;
        public float goodThreshold = 0.09f;
        public float ehThreshold = 0.12f;
    }

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
    public AccuracyCheck earthAccuracyCheck;
    public AccuracyCheck fireAccuracyCheck;

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

    [Header("Spell HUD")]
    [SerializeField] private GameObject waterRuneIcon;
    [SerializeField] private GameObject earthRuneIcon;
    [SerializeField] private GameObject fireRuneIcon;
    [SerializeField] private Color waterRuneReadyColor = Color.white;
    [SerializeField] private Color waterRuneCooldownColor = new Color(1f, 0.25f, 0.25f, 0.55f);

    [Header("Touch Controls")]
    [SerializeField] private bool createTouchSpellButtons = true;
    [SerializeField] private bool alwaysShowTouchSpellButtons = true;
    [SerializeField] private Vector2 touchSpellButtonSize = new Vector2(220f, 92f);
    [SerializeField] private Vector2 touchSpellButtonOffset = new Vector2(48f, 48f);
    [SerializeField] private float touchSpellButtonSpacing = 18f;
    [SerializeField] private Color touchButtonColor = new Color(0.08f, 0.11f, 0.12f, 0.82f);
    [SerializeField] private Color touchButtonSelectedColor = new Color(0.88f, 0.62f, 0.22f, 0.95f);
    [SerializeField] private Color touchButtonDisabledColor = new Color(0.35f, 0.12f, 0.12f, 0.78f);
    [SerializeField] private Vector2 spellGuideSize = new Vector2(320f, 220f);
    [SerializeField] private Vector2 spellGuideOffset = new Vector2(48f, 168f);
    [SerializeField] private Color spellGuidePanelColor = new Color(0.03f, 0.045f, 0.05f, 0.72f);
    [SerializeField] private Color spellGuideLineColor = new Color(0.8f, 0.95f, 1f, 0.9f);
    [SerializeField] private Color spellGuideStartColor = new Color(1f, 0.78f, 0.24f, 0.95f);
    [SerializeField] private Color spellGuideTraceDotColor = new Color(1f, 0.78f, 0.24f, 1f);
    [SerializeField] private float spellGuideTraceDuration = 1.4f;
    private Canvas touchControlsCanvas;
    private Button touchWaterButton;
    private Button touchEarthButton;
    private Button touchFireButton;
    private RectTransform spellGuideRoot;
    private RectTransform spellGuideDrawingRoot;
    private TextMeshProUGUI spellGuideLabel;
    private Image spellGuideStartDot;
    private Image spellGuideTraceDot;
    private readonly List<Image> spellGuideSegments = new List<Image>();
    private Vector2[] currentGuidePoints;
    private float currentGuidePathLength;

    private LineRenderer waterRuneLine;
    private LineRenderer earthRuneLine;
    private LineRenderer fireRuneLine;
    private Vector3[] waterRuneTemplateOffsets;
    private Vector3[] earthRuneTemplateOffsets;
    private Vector3[] fireRuneTemplateOffsets;
    private Vector3[] waterRuneTemplateOffsetsReversed;
    private Vector3[] earthRuneTemplateOffsetsReversed;
    private Vector3[] fireRuneTemplateOffsetsReversed;
    private float waterRuneTemplateAngle;
    private float earthRuneTemplateAngle;
    private float fireRuneTemplateAngle;
    private float waterRuneTemplateAngleReversed;
    private float earthRuneTemplateAngleReversed;
    private float fireRuneTemplateAngleReversed;
    private float waterRuneCurrentAngle;
    private float earthRuneCurrentAngle;
    private float fireRuneCurrentAngle;
    private float waterRuneAngleVelocity;
    private float earthRuneAngleVelocity;
    private float fireRuneAngleVelocity;
    private float referenceCameraOrthographicSize = 5f;
    private float waterRuneBaseWidthMultiplier = 1f;
    private float earthRuneBaseWidthMultiplier = 1f;
    private float fireRuneBaseWidthMultiplier = 1f;
    private bool hasLockedRuneAnchor;
    private Vector3 lockedRuneAnchorPosition;
    private bool hasLockedRuneDirectionMode;
    private bool lockedRuneUsesReversedTemplate;
    private Vector3 lockedRuneStrokeDirection = Vector3.right;
    private bool hideRuneUntilNextStroke;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI spellJudgementText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreRankText;
    [SerializeField] private TextMeshProUGUI missCountText;
    [SerializeField] private Slider scoreMeter;
    [SerializeField] private int scoreMeterMax = 2400;
    [SerializeField] private int score;
    [SerializeField] private int missCount;
    private int perfectCount;
    private int greatCount;
    private int goodCount;
    private int ehCount;
    private int successfulWaterCasts;
    private int stabilizedLacerations;
    private int closedCuts;
    private int closedLacerations;
    [SerializeField] private Vector2 mobileScoreTextOffset = new Vector2(0f, -26f);
    [SerializeField] private Vector2 mobileScoreTextSize = new Vector2(320f, 80f);
    [SerializeField] private Vector2 mobileScoreMeterOffset = new Vector2(0f, -128f);
    [SerializeField] private Vector2 mobileScoreMeterSize = new Vector2(520f, 34f);

    [Header("Accuracy Tuning")]
    [SerializeField] private SpellAccuracyThresholds waterThresholds = new SpellAccuracyThresholds();
    [SerializeField] private SpellAccuracyThresholds earthThresholds = new SpellAccuracyThresholds();
    [SerializeField] private SpellAccuracyThresholds fireThresholds = new SpellAccuracyThresholds();

    [Header("Speed Tuning")]
    [SerializeField] private float idealCastTime = 0.85f;
    [SerializeField] private float maxCastTime = 2.2f;
    [SerializeField] private float speedAccuracyBonus = 0.08f;
    [SerializeField] private float speedAccuracyPenalty = 0.2f;
    [SerializeField] private float quickCastThreshold = 0.95f;

    [Header("Rune Smoothing")]
    [SerializeField] private float runeRotationSmoothTime = 0.12f;
    [SerializeField] private float spellForegroundZ = -8f;
    [SerializeField, Range(0f, 1f)] private float runeCastAlpha = 0.65f;

    [Header("Cast Popup")]
    [SerializeField] private float popupHeightOffset = 1.25f;
    [SerializeField] private Color popupColor = new Color(1f, 0.95f, 0.55f);

    [Header("Water Spell")]
    public float waterPotency = 100f;
    public float waterDuration = 2f;
    public float testAccuracy = 0.38f;
    [SerializeField] private float waterCooldownDuration = 5f;

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
    [SerializeField] private float fireDrawFlameInterval = 0.08f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip waterSFX;
    public AudioClip earthSFX;
    public AudioClip fireSFX;

    private float nextWaterCastTime;
    private float nextFireDrawFlameTime;

    void Awake()
    {
        ApplyDefaultAccuracyThresholds();
        enableDebugHotkeys &= DebugModeState.IsEnabled;
    }

    void Start()
    {
        CacheReferenceCameraSize();

        if (waterRuneVisual == null && waterAccuracyCheck != null)
        {
            waterRuneVisual = waterAccuracyCheck.gameObject;
        }

        if (waterAccuracyCheck != null)
        {
            waterRuneLine = waterAccuracyCheck.GetComponent<LineRenderer>();
            waterRuneBaseWidthMultiplier = GetLineWidthMultiplier(waterRuneLine);
            CacheWaterRuneTemplate();
            ApplyRuneCastOpacity(waterRuneLine);
        }

        if (fireRuneVisual == null && fireAccuracyCheck != null)
        {
            fireRuneVisual = fireAccuracyCheck.gameObject;
        }

        if (earthRuneVisual == null && earthAccuracyCheck != null)
        {
            earthRuneVisual = earthAccuracyCheck.gameObject;
        }

        if (earthAccuracyCheck != null)
        {
            earthRuneLine = earthAccuracyCheck.GetComponent<LineRenderer>();
            earthRuneBaseWidthMultiplier = GetLineWidthMultiplier(earthRuneLine);
            CacheEarthRuneTemplate();
            ApplyRuneCastOpacity(earthRuneLine);
        }

        if (fireAccuracyCheck != null)
        {
            fireRuneLine = fireAccuracyCheck.GetComponent<LineRenderer>();
            fireRuneBaseWidthMultiplier = GetLineWidthMultiplier(fireRuneLine);
            CacheFireRuneTemplate();
            ApplyRuneCastOpacity(fireRuneLine);
        }

        if (requireSpellSelection)
        {
            selectedSpell = SpellType.None;
        }

        EnsureTouchSpellControls();
        SetTouchControlsVisible(!GameplayPause.IsPaused);
        ApplyMobileHudPlacement();
        UpdateRuneVisibility();
        UpdateSpellSelectionUI();
        UpdateScoreUI();
    }

    void ApplyDefaultAccuracyThresholds()
    {
        waterThresholds ??= new SpellAccuracyThresholds();
        earthThresholds ??= new SpellAccuracyThresholds();
        fireThresholds ??= new SpellAccuracyThresholds();

        ApplyThresholdDefaults(waterThresholds, 0.04f, 0.065f, 0.09f, 0.12f);
        ApplyThresholdDefaults(earthThresholds, 0.045f, 0.075f, 0.105f, 0.14f);
        ApplyThresholdDefaults(fireThresholds, 0.05f, 0.08f, 0.115f, 0.15f);
    }

    void ApplyThresholdDefaults(SpellAccuracyThresholds thresholds, float perfect, float great, float good, float eh)
    {
        if (thresholds == null)
        {
            return;
        }

        thresholds.perfectThreshold = perfect;
        thresholds.greatThreshold = great;
        thresholds.goodThreshold = good;
        thresholds.ehThreshold = eh;
    }

    void CacheReferenceCameraSize()
    {
        Camera camera = Camera.main;
        if (camera != null && camera.orthographic && camera.orthographicSize > 0f)
        {
            referenceCameraOrthographicSize = camera.orthographicSize;
        }
    }

    float GetLineWidthMultiplier(LineRenderer runeLine)
    {
        return runeLine != null ? runeLine.widthMultiplier : 1f;
    }

    void Update()
    //todo: implement later
    {
        SetTouchControlsVisible(!GameplayPause.IsPaused);
        ApplyMobileHudPlacement();

        if (GameplayPause.IsPaused)
        {
            return;
        }

        if (patient != null && patientWounds == null)
        {
            patientWounds = patient.GetComponent<PatientWounds>();
        }

        UpdateWaterCooldownVisual();

        if (mouseDraw == null || !mouseDraw.HasStroke)
        {
            hasLockedRuneAnchor = false;
            hasLockedRuneDirectionMode = false;
            lockedRuneStrokeDirection = Vector3.right;
        }

        HandleSpellSelectionInput();

        if ((selectedSpell == SpellType.Water ||
             selectedSpell == SpellType.Earth ||
             selectedSpell == SpellType.Fire) &&
            mouseDraw != null &&
            mouseDraw.TryConsumeStrokeStart(out Vector3 strokeStartPosition))
        {
            hideRuneUntilNextStroke = false;
            UpdateRuneVisibility();
            lockedRuneAnchorPosition = strokeStartPosition;
            hasLockedRuneAnchor = true;
            hasLockedRuneDirectionMode = false;
            StrokeStartIndicator.Create(strokeStartPosition, startIndicatorColor);
        }

        if ((selectedSpell == SpellType.Water ||
             selectedSpell == SpellType.Earth ||
             selectedSpell == SpellType.Fire) &&
            mouseDraw != null &&
            mouseDraw.HasDirectionalStroke &&
            mouseDraw.TryGetStrokeStart(out Vector3 currentStrokeStart))
        {
            EmitFireDrawFlameIfNeeded();

            if (!hasLockedRuneDirectionMode)
            {
                lockedRuneUsesReversedTemplate = ShouldUseReversedTemplate(mouseDraw.CurrentStrokeDirection);
                lockedRuneStrokeDirection = mouseDraw.CurrentStrokeDirection;
                hasLockedRuneDirectionMode = true;
            }

            Vector3 runeAnchorPosition = hasLockedRuneAnchor
                ? lockedRuneAnchorPosition
                : currentStrokeStart;
            MoveSelectedRuneTo(runeAnchorPosition, lockedRuneStrokeDirection, lockedRuneUsesReversedTemplate);
        }

        if (TryConsumeSelectedSpellAccuracy(out float drawnAccuracy))
        {
            hasLockedRuneAnchor = false;
            hasLockedRuneDirectionMode = false;
            lockedRuneStrokeDirection = Vector3.right;
            bool isQuickCast = IsQuickCast();
            Vector3 popupWorldPosition = GetPopupWorldPosition();
            CastSelectedSpell(drawnAccuracy, isQuickCast, popupWorldPosition);
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

        SpellJudgement judgement = GetSpellJudgement(SpellType.Water, acc);
        float mult = GetAccuracyMultiplier(acc);
        float totalHeal = calcWaterHeal(waterPotency, mult);
        int pointsAwarded = GetPointsForJudgement(judgement);
        bool successfulWaterCast = judgement != SpellJudgement.Miss;

        float t = Mathf.InverseLerp(0.1f, 1.1f, mult);
        float reduction = Mathf.Lerp(0.6f, 0.3f, t);

        if (successfulWaterCast)
        {
            patient.startHoT(totalHeal, waterDuration);
            patient.bleedReduction(reduction, waterDuration); //reduces bleed based on accuracy of the spell
            SpellVisualEffects.PlayWaterSplash(GetWaterSplashPosition(popupWorldPosition));
            nextWaterCastTime = Time.time + waterCooldownDuration;
            score += pointsAwarded;
            successfulWaterCasts++;
            RegisterJudgementScore(judgement);
        }
        else
        {
            totalHeal = 0f;
            reduction = 1f;
        }

        RegisterMissIfNeeded(judgement);

        UpdateFeedbackUI(judgement);
        if (showPopup)
        {
            ShowCastPopup(judgement, isQuickCast, popupWorldPosition);
        }


        if(audioSource != null && waterSFX != null)
        {
            audioSource.PlayOneShot(waterSFX);
        }

        if (successfulWaterCast)
        {
            HideRuneAfterSuccessfulCast();
            SpellCastSucceeded?.Invoke(SpellType.Water);
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
        hideRuneUntilNextStroke = false;
        selectedSpell = spell;
        UpdateRuneVisibility();
        UpdateSpellSelectionUI();
        SpellSelected?.Invoke(spell);
    }

    public SpellType GetSelectedSpell()
    {
        return selectedSpell;
    }

    public int GetScore()
    {
        return score;
    }

    public int GetMissCount()
    {
        return missCount;
    }

    public string GetCurrentScoreRank()
    {
        return GetScoreRankText();
    }

    public string GetScoreBreakdown()
    {
        return "Scoring: Perfect 300, Great 250, Good 150, Eh 50. Misses lower rank.";
    }

    public string GetMissionCompleteBreakdown()
    {
        return
            "Score sources\n" +
            "Perfect " + perfectCount + "  Great " + greatCount + "  Good " + goodCount + "  Eh " + ehCount + "\n" +
            "Cleaned\n" +
            "Cuts closed " + closedCuts + "  Lacerations stabilised " + stabilizedLacerations + "  Lacerations closed " + closedLacerations + "\n" +
            "Water casts " + successfulWaterCasts + "  Misses " + missCount;
    }

    public string GetGameOverBreakdown()
    {
        return
            "Score sources\n" +
            "Perfect " + perfectCount + "  Great " + greatCount + "  Good " + goodCount + "  Eh " + ehCount + "\n" +
            "Progress\n" +
            "Cuts closed " + closedCuts + "  Lacerations stabilised " + stabilizedLacerations + "  Lacerations closed " + closedLacerations + "\n" +
            "Water casts " + successfulWaterCasts + "  Misses " + missCount;
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
                if (IsWaterOnCooldown())
                {
                    ShowWaterCooldownFeedback();
                    return;
                }

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

    SpellJudgement GetSpellJudgement(SpellType spellType, float acc)
    {
        SpellAccuracyThresholds thresholds = GetThresholdsForSpell(spellType);

        if (acc <= thresholds.perfectThreshold)
        {
            return SpellJudgement.Nice;
        }

        if (acc <= thresholds.greatThreshold)
        {
            return SpellJudgement.Great;
        }

        if (acc <= thresholds.goodThreshold)
        {
            return SpellJudgement.Good;
        }

        if (acc <= thresholds.ehThreshold)
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
                return "PERFECT!!!! (300 points)";
            case SpellJudgement.Great:
                return "GREAT!!! (250 points)";
            case SpellJudgement.Good:
                return "GOOD!! (150 points)";
            case SpellJudgement.Eh:
                return "EH...! (50 points)";
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

        UpdateScoreUI();
    }

    void EmitFireDrawFlameIfNeeded()
    {
        if (selectedSpell != SpellType.Fire ||
            mouseDraw == null ||
            !mouseDraw.HasStroke ||
            Time.unscaledTime < nextFireDrawFlameTime)
        {
            return;
        }

        nextFireDrawFlameTime = Time.unscaledTime + fireDrawFlameInterval;
        SpellVisualEffects.PlayFireDrawFlame(mouseDraw.LastStrokeEndWorldPosition);
    }

    float GetAccuracyMultiplier(float acc)
    {
        SpellType spellType = selectedSpell == SpellType.None ? SpellType.Water : selectedSpell;
        SpellAccuracyThresholds thresholds = GetThresholdsForSpell(spellType);

        if (acc <= thresholds.perfectThreshold)
        {
            return 1.1f;
        }

        if (acc <= thresholds.greatThreshold)
        {
            return 0.85f;
        }

        if (acc <= thresholds.goodThreshold)
        {
            return 0.7f;
        }

        if (acc <= thresholds.ehThreshold)
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
            RegisterMissIfNeeded(SpellJudgement.Miss);
            ShowCastPopup(SpellJudgement.Miss, IsQuickCast(), GetPopupWorldPosition());
            UpdateScoreUI();

            if (spellJudgementText != null)
            {
                spellJudgementText.text = "Draw the spell over a wound.";
            }

            Debug.Log(spellType + " cast failed: no wound targeted.");
            return;
        }

        SpellJudgement judgement = GetSpellJudgement(spellType, acc);
        int pointsAwarded = GetPointsForJudgement(judgement);
        bool treated = false;
        CutWound.WoundType woundTypeBeforeCast = wound.Type;
        Vector3 targetEffectPosition = GetWoundEffectPosition(wound);
        float targetEffectRadius = GetWoundEffectRadius(wound);
        string outcome;

        if (judgement == SpellJudgement.Miss)
        {
            outcome = spellType + " missed.";
        }
        else
        {
            treated = wound.TryApplySpell(spellType, out outcome);
        }

        SpellJudgement displayedJudgement = treated ? judgement : SpellJudgement.Miss;
        RegisterMissIfNeeded(displayedJudgement);

        if (treated)
        {
            score += pointsAwarded;
            RegisterJudgementScore(judgement);
            RegisterWoundCleanup(spellType, woundTypeBeforeCast);
            HideRuneAfterSuccessfulCast();
            PlaySpellSfx(spellType);
            PlayTargetedSpellEffect(spellType, wound, targetEffectPosition, targetEffectRadius);
            SpellCastSucceeded?.Invoke(spellType);
        }

        ShowCastPopup(displayedJudgement, IsQuickCast(), GetPopupWorldPosition());

        if (spellJudgementText != null)
        {
            spellJudgementText.text = outcome;
        }

        UpdateScoreUI();

        Debug.Log(
            spellType + " cast - acc: " + acc.ToString("F3") +
            " - outcome: " + outcome +
            " - points: " + (treated ? pointsAwarded : 0));
    }

    Vector3 GetWaterSplashPosition(Vector3 popupWorldPosition)
    {
        if (popupWorldPosition != Vector3.zero)
        {
            return popupWorldPosition - (Vector3.up * popupHeightOffset);
        }

        if (mouseDraw != null && mouseDraw.LastStrokeEndWorldPosition != Vector3.zero)
        {
            return mouseDraw.LastStrokeEndWorldPosition;
        }

        return patient != null ? patient.transform.position : transform.position;
    }

    Vector3 GetWoundEffectPosition(CutWound wound)
    {
        SpriteRenderer renderer = wound != null ? wound.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (renderer != null)
        {
            return renderer.bounds.center;
        }

        return wound != null ? wound.transform.position : transform.position;
    }

    float GetWoundEffectRadius(CutWound wound)
    {
        SpriteRenderer renderer = wound != null ? wound.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (renderer == null)
        {
            return 0.25f;
        }

        Vector3 extents = renderer.bounds.extents;
        return Mathf.Max(0.12f, Mathf.Max(extents.x, extents.y));
    }

    void PlayTargetedSpellEffect(SpellType spellType, CutWound wound, Vector3 position, float radius)
    {
        switch (spellType)
        {
            case SpellType.Earth:
                SpellVisualEffects.PlayEarthSqueeze(wound);
                break;
            case SpellType.Fire:
                SpellVisualEffects.PlayFireDots(position, radius);
                break;
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (scoreRankText != null)
        {
            scoreRankText.text = GetScoreRankText();
        }

        if (missCountText != null)
        {
            missCountText.text = "Misses: " + missCount;
        }

        if (scoreMeter != null)
        {
            scoreMeter.maxValue = Mathf.Max(1, scoreMeterMax);
            scoreMeter.value = Mathf.Clamp(score, 0, scoreMeter.maxValue);
        }
    }

    void ApplyMobileHudPlacement()
    {
        if (!Application.isMobilePlatform && Screen.width <= Screen.height)
        {
            return;
        }

        PlaceTopCenterText(scoreText, mobileScoreTextOffset, mobileScoreTextSize);
        PlaceTopCenterText(scoreRankText, mobileScoreTextOffset + new Vector2(0f, -46f), new Vector2(520f, 56f));
        PlaceTopCenterText(missCountText, mobileScoreTextOffset + new Vector2(0f, -92f), new Vector2(360f, 48f));

        if (scoreMeter != null)
        {
            RectTransform meterRect = scoreMeter.transform as RectTransform;
            PlaceTopCenterRect(meterRect, mobileScoreMeterOffset, mobileScoreMeterSize);
        }
    }

    void PlaceTopCenterText(TextMeshProUGUI text, Vector2 offset, Vector2 size)
    {
        if (text == null)
        {
            return;
        }

        PlaceTopCenterRect(text.rectTransform, offset, size);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
    }

    void PlaceTopCenterRect(RectTransform rect, Vector2 offset, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect != null && rect.parent != canvasRect)
        {
            rect.SetParent(canvasRect, false);
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        rect.SetAsLastSibling();
    }

    void RegisterMissIfNeeded(SpellJudgement judgement)
    {
        if (judgement == SpellJudgement.Miss)
        {
            missCount++;
        }
    }

    void RegisterJudgementScore(SpellJudgement judgement)
    {
        switch (judgement)
        {
            case SpellJudgement.Nice:
                perfectCount++;
                break;
            case SpellJudgement.Great:
                greatCount++;
                break;
            case SpellJudgement.Good:
                goodCount++;
                break;
            case SpellJudgement.Eh:
                ehCount++;
                break;
        }
    }

    void RegisterWoundCleanup(SpellType spellType, CutWound.WoundType woundType)
    {
        if (spellType == SpellType.Earth && woundType == CutWound.WoundType.Laceration)
        {
            stabilizedLacerations++;
            return;
        }

        if (spellType != SpellType.Fire)
        {
            return;
        }

        if (woundType == CutWound.WoundType.Cut)
        {
            closedCuts++;
        }
        else if (woundType == CutWound.WoundType.Laceration)
        {
            closedLacerations++;
        }
    }

    string GetScoreRankText()
    {
        if (score >= 2200 && missCount <= 1)
        {
            return "MS - Master Sage";
        }

        if (score >= 1900 && missCount <= 2)
        {
            return "S - Supreme Sage";
        }

        if (score >= 1500 && missCount <= 4)
        {
            return "A - Adept Sage";
        }

        if (score >= 1100)
        {
            return "B - Beneficial Sage";
        }

        if (score >= 700)
        {
            return "C - Rookie Sage";
        }

        return "D - Desperate Sage";
    }

    bool TryGetTargetWound(out CutWound wound)
    {
        wound = null;

        if (patientWounds == null)
        {
            return false;
        }

        return mouseDraw != null &&
               mouseDraw.CurrentLine != null &&
               patientWounds.TryGetWoundTouchedByLine(mouseDraw.CurrentLine, out wound);
    }

    Vector3 GetRuneAnchorPosition(Vector3 fallbackPosition)
    {
        if (patientWounds == null)
        {
            return fallbackPosition;
        }

        if (mouseDraw != null &&
            mouseDraw.CurrentLine != null &&
            patientWounds.TryGetWoundTouchedByLine(mouseDraw.CurrentLine, out CutWound lineTouchedWound))
        {
            return lineTouchedWound.GetSpellAnchorPosition(fallbackPosition);
        }

        if (patientWounds.TryGetWoundAtSpellPoint(fallbackPosition, out CutWound pointTouchedWound))
        {
            return pointTouchedWound.GetSpellAnchorPosition(fallbackPosition);
        }

        return fallbackPosition;
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

        float adjustedAccuracy = baseAccuracy;

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
            Vector3 fallbackPosition = transform.position + (Vector3.up * GetScaledPopupHeightOffset());
            fallbackPosition.z = spellForegroundZ;
            return fallbackPosition;
        }

        Vector3 popupPosition = mouseDraw.LastStrokeEndWorldPosition + (Vector3.up * GetScaledPopupHeightOffset());
        popupPosition.z = spellForegroundZ;
        return popupPosition;
    }

    float GetScaledPopupHeightOffset()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic || referenceCameraOrthographicSize <= 0f)
        {
            return popupHeightOffset;
        }

        float cameraScale = Mathf.Clamp(camera.orthographicSize / referenceCameraOrthographicSize, 0.42f, 1.25f);
        return popupHeightOffset * cameraScale;
    }

    void ShowCastPopup(SpellJudgement judgement, bool isQuickCast, Vector3 popupWorldPosition)
    {
        SpellCastPopup.Create(popupWorldPosition, GetPopupText(judgement), popupColor);
    }

    string GetPopupText(SpellJudgement judgement)
    {
        switch (judgement)
        {
            case SpellJudgement.Nice:
                return "PERFECT!!!!";
            case SpellJudgement.Great:
                return "GREAT!!!";
            case SpellJudgement.Good:
                return "GOOD!!";
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
        bool showSelectedRune = !hideRuneUntilNextStroke;
        SetRuneVisible(waterRuneVisual, selectedSpell == SpellType.Water && showSelectedRune);
        SetRuneVisible(earthRuneVisual, selectedSpell == SpellType.Earth && showSelectedRune);
        SetRuneVisible(fireRuneVisual, selectedSpell == SpellType.Fire && showSelectedRune);
    }

    void UpdateSpellSelectionUI()
    {
        SetRuneVisible(waterRuneIcon, selectedSpell == SpellType.Water);
        SetRuneVisible(earthRuneIcon, selectedSpell == SpellType.Earth);
        SetRuneVisible(fireRuneIcon, selectedSpell == SpellType.Fire);
        UpdateWaterCooldownVisual();
        UpdateTouchSpellButtonStates();
        UpdateSpellGuide();
        AnimateSpellGuideTraceDot();
    }

    void SetRuneVisible(GameObject runeVisual, bool visible)
    {
        if (runeVisual != null)
        {
            runeVisual.SetActive(visible);
        }
    }

    void ApplyRuneCastOpacity(LineRenderer runeLine)
    {
        if (runeLine == null)
        {
            return;
        }

        Gradient gradient = runeLine.colorGradient;
        GradientColorKey[] colorKeys = gradient.colorKeys;
        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
        float alpha = Mathf.Clamp01(runeCastAlpha);

        if (alphaKeys == null || alphaKeys.Length == 0)
        {
            alphaKeys = new[]
            {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(alpha, 1f)
            };
        }
        else
        {
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = alpha;
            }
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        runeLine.colorGradient = gradient;
    }

    void CacheWaterRuneTemplate()
    {
        if (waterRuneLine == null || waterRuneLine.positionCount == 0)
        {
            waterRuneTemplateOffsets = null;
            waterRuneTemplateAngle = 0f;
            return;
        }

        waterRuneTemplateOffsets = new Vector3[waterRuneLine.positionCount];
        Vector3 anchor = waterRuneLine.GetPosition(0);
        waterRuneTemplateAngle = GetFirstSegmentAngle(waterRuneLine, false);
        waterRuneTemplateOffsetsReversed = GetReversedOffsets(waterRuneLine);
        waterRuneTemplateAngleReversed = GetFirstSegmentAngle(waterRuneLine, true);

        for (int i = 0; i < waterRuneLine.positionCount; i++)
        {
            waterRuneTemplateOffsets[i] = waterRuneLine.GetPosition(i) - anchor;
        }

        waterRuneCurrentAngle = waterRuneTemplateAngle;
        waterRuneAngleVelocity = 0f;
    }

    void CacheFireRuneTemplate()
    {
        if (fireRuneLine == null || fireRuneLine.positionCount == 0)
        {
            fireRuneTemplateOffsets = null;
            fireRuneTemplateAngle = 0f;
            return;
        }

        fireRuneTemplateOffsets = new Vector3[fireRuneLine.positionCount];
        Vector3 anchor = fireRuneLine.GetPosition(0);
        fireRuneTemplateAngle = GetFirstSegmentAngle(fireRuneLine, false);
        fireRuneTemplateOffsetsReversed = GetReversedOffsets(fireRuneLine);
        fireRuneTemplateAngleReversed = GetFirstSegmentAngle(fireRuneLine, true);

        for (int i = 0; i < fireRuneLine.positionCount; i++)
        {
            fireRuneTemplateOffsets[i] = fireRuneLine.GetPosition(i) - anchor;
        }

        fireRuneCurrentAngle = fireRuneTemplateAngle;
        fireRuneAngleVelocity = 0f;
    }

    void CacheEarthRuneTemplate()
    {
        if (earthRuneLine == null || earthRuneLine.positionCount == 0)
        {
            earthRuneTemplateOffsets = null;
            earthRuneTemplateAngle = 0f;
            return;
        }

        earthRuneTemplateOffsets = new Vector3[earthRuneLine.positionCount];
        Vector3 anchor = earthRuneLine.GetPosition(0);
        earthRuneTemplateAngle = GetFirstSegmentAngle(earthRuneLine, false);
        earthRuneTemplateOffsetsReversed = GetReversedOffsets(earthRuneLine);
        earthRuneTemplateAngleReversed = GetFirstSegmentAngle(earthRuneLine, true);

        for (int i = 0; i < earthRuneLine.positionCount; i++)
        {
            earthRuneTemplateOffsets[i] = earthRuneLine.GetPosition(i) - anchor;
        }

        earthRuneCurrentAngle = earthRuneTemplateAngle;
        earthRuneAngleVelocity = 0f;
    }

    void MoveSelectedRuneTo(Vector3 anchorPosition)
    {
        MoveSelectedRuneTo(anchorPosition, Vector3.right, false);
    }

    void MoveSelectedRuneTo(Vector3 anchorPosition, Vector3 strokeDirection, bool useReversedTemplate)
    {
        switch (selectedSpell)
        {
            case SpellType.Water:
                MoveRuneTo(
                    waterRuneLine,
                    useReversedTemplate ? waterRuneTemplateOffsetsReversed : waterRuneTemplateOffsets,
                    anchorPosition,
                    useReversedTemplate ? waterRuneTemplateAngleReversed : waterRuneTemplateAngle,
                    strokeDirection,
                    waterRuneBaseWidthMultiplier,
                    ref waterRuneCurrentAngle,
                    ref waterRuneAngleVelocity);
                break;
            case SpellType.Earth:
                MoveRuneTo(
                    earthRuneLine,
                    useReversedTemplate ? earthRuneTemplateOffsetsReversed : earthRuneTemplateOffsets,
                    anchorPosition,
                    useReversedTemplate ? earthRuneTemplateAngleReversed : earthRuneTemplateAngle,
                    strokeDirection,
                    earthRuneBaseWidthMultiplier,
                    ref earthRuneCurrentAngle,
                    ref earthRuneAngleVelocity);
                break;
            case SpellType.Fire:
                MoveRuneTo(
                    fireRuneLine,
                    useReversedTemplate ? fireRuneTemplateOffsetsReversed : fireRuneTemplateOffsets,
                    anchorPosition,
                    useReversedTemplate ? fireRuneTemplateAngleReversed : fireRuneTemplateAngle,
                    strokeDirection,
                    fireRuneBaseWidthMultiplier,
                    ref fireRuneCurrentAngle,
                    ref fireRuneAngleVelocity);
                break;
        }
    }

    bool TryConsumeSelectedSpellAccuracy(out float accuracy)
    {
        accuracy = 0f;

        if (!useDrawnWaterSpell)
        {
            return false;
        }

        switch (selectedSpell)
        {
            case SpellType.Water:
                return TryConsumeAdjustedAccuracy(waterAccuracyCheck, out accuracy);
            case SpellType.Earth:
                return TryConsumeAdjustedAccuracy(earthAccuracyCheck, out accuracy);
            case SpellType.Fire:
                return TryConsumeAdjustedAccuracy(fireAccuracyCheck, out accuracy);
            default:
                return false;
        }
    }

    bool TryConsumeAdjustedAccuracy(AccuracyCheck accuracyCheck, out float accuracy)
    {
        accuracy = 0f;

        if (accuracyCheck == null ||
            !accuracyCheck.TryConsumeAccuracy(out float rawAccuracy))
        {
            return false;
        }

        accuracy = GetSpeedAdjustedAccuracy(rawAccuracy);
        return true;
    }

    float GetFirstSegmentAngle(LineRenderer runeLine, bool reversed)
    {
        if (runeLine == null || runeLine.positionCount < 2)
        {
            return 0f;
        }

        int startIndex = reversed ? runeLine.positionCount - 1 : 0;
        int nextIndex = reversed ? runeLine.positionCount - 2 : 1;

        Vector3 start = runeLine.GetPosition(startIndex);
        Vector3 next = runeLine.GetPosition(nextIndex);
        Vector3 direction = next - start;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    Vector3[] GetReversedOffsets(LineRenderer runeLine)
    {
        if (runeLine == null || runeLine.positionCount == 0)
        {
            return null;
        }

        Vector3[] reversedOffsets = new Vector3[runeLine.positionCount];
        Vector3 reverseAnchor = runeLine.GetPosition(runeLine.positionCount - 1);

        for (int i = 0; i < runeLine.positionCount; i++)
        {
            int reverseIndex = runeLine.positionCount - i - 1;
            reversedOffsets[i] = runeLine.GetPosition(reverseIndex) - reverseAnchor;
        }

        return reversedOffsets;
    }

    bool ShouldUseReversedTemplate(Vector3 strokeDirection)
    {
        if (strokeDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        float strokeAngle = Mathf.Atan2(strokeDirection.y, strokeDirection.x) * Mathf.Rad2Deg;
        float forwardDelta = Mathf.Abs(Mathf.DeltaAngle(GetSelectedRuneTemplateAngle(false), strokeAngle));
        float reverseDelta = Mathf.Abs(Mathf.DeltaAngle(GetSelectedRuneTemplateAngle(true), strokeAngle));
        return reverseDelta < forwardDelta;
    }

    float GetSelectedRuneTemplateAngle(bool reversed)
    {
        switch (selectedSpell)
        {
            case SpellType.Water:
                return reversed ? waterRuneTemplateAngleReversed : waterRuneTemplateAngle;
            case SpellType.Earth:
                return reversed ? earthRuneTemplateAngleReversed : earthRuneTemplateAngle;
            case SpellType.Fire:
                return reversed ? fireRuneTemplateAngleReversed : fireRuneTemplateAngle;
            default:
                return 0f;
        }
    }

    void MoveRuneTo(
        LineRenderer runeLine,
        Vector3[] runeTemplateOffsets,
        Vector3 anchorPosition,
        float templateAngle,
        Vector3 strokeDirection,
        float baseWidthMultiplier,
        ref float currentAngle,
        ref float angleVelocity)
    {
        if (runeLine == null || runeTemplateOffsets == null)
        {
            return;
        }

        float targetAngle = templateAngle;
        if (strokeDirection.sqrMagnitude > Mathf.Epsilon)
        {
            targetAngle = Mathf.Atan2(strokeDirection.y, strokeDirection.x) * Mathf.Rad2Deg;
        }

        currentAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref angleVelocity,
            runeRotationSmoothTime);

        Quaternion rotation = Quaternion.Euler(0f, 0f, currentAngle - templateAngle);
        float cameraScale = GetRuneCameraScale();
        runeLine.widthMultiplier = baseWidthMultiplier * cameraScale;

        for (int i = 0; i < runeTemplateOffsets.Length; i++)
        {
            Vector3 point = anchorPosition + (rotation * (runeTemplateOffsets[i] * cameraScale));
            point.z = spellForegroundZ;
            runeLine.SetPosition(i, point);
        }
    }

    float GetRuneCameraScale()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic || referenceCameraOrthographicSize <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, camera.orthographicSize / referenceCameraOrthographicSize);
    }

    SpellAccuracyThresholds GetThresholdsForSpell(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.Earth:
                return earthThresholds;
            case SpellType.Fire:
                return fireThresholds;
            case SpellType.Water:
            default:
                return waterThresholds;
        }
    }

    void HideRuneAfterSuccessfulCast()
    {
        hideRuneUntilNextStroke = true;
        hasLockedRuneAnchor = false;
        hasLockedRuneDirectionMode = false;
        lockedRuneStrokeDirection = Vector3.right;
        UpdateRuneVisibility();
    }

    bool IsWaterOnCooldown()
    {
        return Time.time < nextWaterCastTime;
    }

    void UpdateWaterCooldownVisual()
    {
        float remainingCooldown = Mathf.Max(0f, nextWaterCastTime - Time.time);
        Color targetColor = waterRuneReadyColor;

        if (remainingCooldown > 0f && waterCooldownDuration > 0f)
        {
            float cooldownProgress = 1f - Mathf.Clamp01(remainingCooldown / waterCooldownDuration);
            targetColor = Color.Lerp(waterRuneCooldownColor, waterRuneReadyColor, cooldownProgress);
        }

        ApplyIconColor(waterRuneIcon, targetColor);
        UpdateTouchSpellButtonStates();
    }

    void ShowWaterCooldownFeedback()
    {
        float remainingCooldown = Mathf.Max(0f, nextWaterCastTime - Time.time);

        if (spellJudgementText != null)
        {
            spellJudgementText.text = "Water cooling down: " + remainingCooldown.ToString("F1") + "s";
        }
    }

    void PlaySpellSfx(SpellType spellType)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clipToPlay = null;

        switch (spellType)
        {
            case SpellType.Water:
                clipToPlay = waterSFX;
                break;
            case SpellType.Earth:
                clipToPlay = earthSFX;
                break;
            case SpellType.Fire:
                clipToPlay = fireSFX;
                break;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    void ApplyIconColor(GameObject iconObject, Color color)
    {
        if (iconObject == null)
        {
            return;
        }

        Graphic graphic = iconObject.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = color;
        }

        SpriteRenderer spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    void EnsureTouchSpellControls()
    {
        if (!ShouldCreateTouchSpellControls() || touchControlsCanvas != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("TouchSpellControls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        touchControlsCanvas = canvasObject.GetComponent<Canvas>();
        touchControlsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        touchControlsCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        touchWaterButton = CreateTouchSpellButton(root, "TouchWaterSpellButton", "Water", 0, SpellType.Water);
        touchEarthButton = CreateTouchSpellButton(root, "TouchEarthSpellButton", "Earth", 1, SpellType.Earth);
        touchFireButton = CreateTouchSpellButton(root, "TouchFireSpellButton", "Fire", 2, SpellType.Fire);
        CreateSpellGuide(root);
        UpdateSpellGuide();
    }

    void SetTouchControlsVisible(bool visible)
    {
        if (touchControlsCanvas != null && touchControlsCanvas.gameObject.activeSelf != visible)
        {
            touchControlsCanvas.gameObject.SetActive(visible);
        }
    }

    bool ShouldCreateTouchSpellControls()
    {
        return createTouchSpellButtons &&
               (alwaysShowTouchSpellButtons || Application.isMobilePlatform || Input.touchSupported);
    }

    Button CreateTouchSpellButton(RectTransform parent, string objectName, string labelText, int index, SpellType spellType)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = touchSpellButtonSize;
        rect.anchoredPosition = touchSpellButtonOffset + new Vector2(index * (touchSpellButtonSize.x + touchSpellButtonSpacing), 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = touchButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SetSelectedSpell(spellType));

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(rect, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 34f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    void CreateSpellGuide(RectTransform parent)
    {
        if (parent == null || spellGuideRoot != null)
        {
            return;
        }

        GameObject guideObject = new GameObject("SpellShapeGuide", typeof(RectTransform), typeof(Image));
        guideObject.transform.SetParent(parent, false);

        spellGuideRoot = guideObject.GetComponent<RectTransform>();
        spellGuideRoot.anchorMin = new Vector2(0f, 0f);
        spellGuideRoot.anchorMax = new Vector2(0f, 0f);
        spellGuideRoot.pivot = new Vector2(0f, 0f);
        spellGuideRoot.sizeDelta = spellGuideSize;
        spellGuideRoot.anchoredPosition = spellGuideOffset;

        Image panelImage = guideObject.GetComponent<Image>();
        panelImage.color = spellGuidePanelColor;
        panelImage.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(spellGuideRoot, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(12f, -44f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        spellGuideLabel = labelObject.GetComponent<TextMeshProUGUI>();
        spellGuideLabel.fontSize = 26f;
        spellGuideLabel.alignment = TextAlignmentOptions.Center;
        spellGuideLabel.color = Color.white;
        spellGuideLabel.enableWordWrapping = false;
        spellGuideLabel.raycastTarget = false;

        GameObject drawingObject = new GameObject("Drawing", typeof(RectTransform));
        drawingObject.transform.SetParent(spellGuideRoot, false);
        spellGuideDrawingRoot = drawingObject.GetComponent<RectTransform>();
        spellGuideDrawingRoot.anchorMin = new Vector2(0f, 0f);
        spellGuideDrawingRoot.anchorMax = new Vector2(1f, 1f);
        spellGuideDrawingRoot.offsetMin = new Vector2(22f, 20f);
        spellGuideDrawingRoot.offsetMax = new Vector2(-22f, -56f);

        spellGuideStartDot = CreateGuideDot(spellGuideDrawingRoot, "StartDot", spellGuideStartColor);
        spellGuideTraceDot = CreateGuideDot(spellGuideDrawingRoot, "TraceDot", spellGuideTraceDotColor);
        spellGuideTraceDot.rectTransform.sizeDelta = new Vector2(14f, 14f);
        spellGuideRoot.gameObject.SetActive(false);
    }

    Image CreateGuideDot(RectTransform parent, string objectName, Color color)
    {
        GameObject dotObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        dotObject.transform.SetParent(parent, false);
        RectTransform rect = dotObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(18f, 18f);

        Image image = dotObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void UpdateSpellGuide()
    {
        if (spellGuideRoot == null || spellGuideDrawingRoot == null)
        {
            return;
        }

        Vector3[] template = GetSelectedGuideTemplate();
        bool showGuide = selectedSpell == SpellType.Water ||
                         selectedSpell == SpellType.Earth ||
                         selectedSpell == SpellType.Fire;

        spellGuideRoot.gameObject.SetActive(showGuide && template != null && template.Length >= 2);
        if (!spellGuideRoot.gameObject.activeSelf)
        {
            return;
        }

        if (spellGuideLabel != null)
        {
            spellGuideLabel.text = selectedSpell.ToString();
        }

        currentGuidePoints = GetGuidePoints(template, spellGuideDrawingRoot.rect.size);
        currentGuidePathLength = GetGuidePathLength(currentGuidePoints);
        EnsureGuideSegmentCount(currentGuidePoints.Length - 1);

        for (int i = 0; i < spellGuideSegments.Count; i++)
        {
            bool active = i < currentGuidePoints.Length - 1;
            spellGuideSegments[i].gameObject.SetActive(active);
            if (active)
            {
                PositionGuideSegment(spellGuideSegments[i].rectTransform, currentGuidePoints[i], currentGuidePoints[i + 1]);
            }
        }

        if (spellGuideStartDot != null)
        {
            spellGuideStartDot.rectTransform.anchoredPosition = currentGuidePoints[0];
            spellGuideStartDot.transform.SetAsLastSibling();
        }

        if (spellGuideTraceDot != null)
        {
            spellGuideTraceDot.transform.SetAsLastSibling();
            AnimateSpellGuideTraceDot();
        }
    }

    Vector3[] GetSelectedGuideTemplate()
    {
        switch (selectedSpell)
        {
            case SpellType.Water:
                return waterRuneTemplateOffsets;
            case SpellType.Earth:
                return earthRuneTemplateOffsets;
            case SpellType.Fire:
                return fireRuneTemplateOffsets;
            default:
                return null;
        }
    }

    Vector2[] GetGuidePoints(Vector3[] template, Vector2 drawingSize)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < template.Length; i++)
        {
            Vector2 point = template[i];
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        Vector2 boundsSize = max - min;
        float scale = Mathf.Min(
            drawingSize.x / Mathf.Max(0.01f, boundsSize.x),
            drawingSize.y / Mathf.Max(0.01f, boundsSize.y)) * 0.78f;
        Vector2 centerOffset = drawingSize * 0.5f;
        Vector2 boundsCenter = (min + max) * 0.5f;
        Vector2[] points = new Vector2[template.Length];

        for (int i = 0; i < template.Length; i++)
        {
            points[i] = ((Vector2)template[i] - boundsCenter) * scale + centerOffset;
        }

        return points;
    }

    void EnsureGuideSegmentCount(int requiredCount)
    {
        while (spellGuideSegments.Count < requiredCount)
        {
            GameObject segmentObject = new GameObject("Segment", typeof(RectTransform), typeof(Image));
            segmentObject.transform.SetParent(spellGuideDrawingRoot, false);

            Image image = segmentObject.GetComponent<Image>();
            image.color = spellGuideLineColor;
            image.raycastTarget = false;
            spellGuideSegments.Add(image);
        }
    }

    void PositionGuideSegment(RectTransform rect, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float length = Mathf.Max(1f, delta.magnitude);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (start + end) * 0.5f;
        rect.sizeDelta = new Vector2(length, 6f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    float GetGuidePathLength(Vector2[] points)
    {
        if (points == null || points.Length < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            length += Vector2.Distance(points[i - 1], points[i]);
        }

        return length;
    }

    void AnimateSpellGuideTraceDot()
    {
        if (spellGuideRoot == null ||
            !spellGuideRoot.gameObject.activeSelf ||
            spellGuideTraceDot == null ||
            currentGuidePoints == null ||
            currentGuidePoints.Length < 2 ||
            currentGuidePathLength <= Mathf.Epsilon)
        {
            return;
        }

        float duration = Mathf.Max(0.1f, spellGuideTraceDuration);
        float targetDistance = Mathf.Repeat(Time.unscaledTime / duration, 1f) * currentGuidePathLength;
        float traversed = 0f;

        for (int i = 1; i < currentGuidePoints.Length; i++)
        {
            Vector2 start = currentGuidePoints[i - 1];
            Vector2 end = currentGuidePoints[i];
            float segmentLength = Vector2.Distance(start, end);
            if (traversed + segmentLength >= targetDistance)
            {
                float t = segmentLength <= Mathf.Epsilon ? 0f : (targetDistance - traversed) / segmentLength;
                spellGuideTraceDot.rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
                return;
            }

            traversed += segmentLength;
        }

        spellGuideTraceDot.rectTransform.anchoredPosition = currentGuidePoints[currentGuidePoints.Length - 1];
    }

    void UpdateTouchSpellButtonStates()
    {
        UpdateTouchSpellButton(touchWaterButton, SpellType.Water, IsWaterOnCooldown());
        UpdateTouchSpellButton(touchEarthButton, SpellType.Earth, false);
        UpdateTouchSpellButton(touchFireButton, SpellType.Fire, false);
    }

    void UpdateTouchSpellButton(Button button, SpellType spellType, bool disabled)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = true;

        Graphic graphic = button.targetGraphic;
        if (graphic == null)
        {
            return;
        }

        if (disabled)
        {
            graphic.color = touchButtonDisabledColor;
        }
        else
        {
            graphic.color = selectedSpell == spellType ? touchButtonSelectedColor : touchButtonColor;
        }
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
