using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PatientDialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;

    public bool waitForSpellSelect;
    public bool waitForSpellCast;
    public bool waitForWoundsCleared;
    public bool waitBeforeShowingLine;
    public bool transitionToInside;
    public bool transitionToPart;
    public string focusSpawnAreaId;
    public string activateSpawnAreaId;
    public bool triggerEmergencyTimeDilation;
    public bool triggerEmergencyImpact;
    public bool missionCompleteFlag;
    public SpellController.SpellType requiredSpell = SpellController.SpellType.None;
    public CutWound.WoundLocation requiredWoundLocation = CutWound.WoundLocation.Outside;
    public string requiredSpawnAreaId;
}

public class PatientDialogueController : MonoBehaviour
{
    [SerializeField] private SpellController spellController;
    [SerializeField] private PatientWounds patientWounds;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private GameObject nextButtonRoot;
    [SerializeField] private PatientDialogueLine[] lines;
    [SerializeField] private float characterRevealInterval = 0.05f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceTestClip;
    [SerializeField] private SurgeryEndController surgeryEndController;
    [Header("Patient View Transition")]
    [SerializeField] private GameObject outsidePatientRoot;
    [SerializeField] private GameObject insidePatientRoot;
    [SerializeField] private GameObject partPatientRoot;
    [SerializeField] private Image transitionFadeOverlay;
    [SerializeField] private float transitionFadeDuration = 0.35f;
    [SerializeField] private float transitionMidpointHoldDuration = 0.1f;
    private int currentLineIndex;
    private Coroutine typewriterRoutine;
    private Coroutine transitionRoutine;
    private bool isTyping;
    private bool isWaitingForSpellSelect;
    private bool isWaitingForSpellCast;
    private bool isWaitingForWoundsCleared;
    private bool isWaitingBeforeShowingLine;
    private bool hasMatchedSpellSelection;
    private bool hasMatchedSpellCast;
    private bool hasTransitionedInside;
    private bool hasTransitionedToPart;
    private int lastAdvanceFrame = -1;
    private GameObject currentPatientRoot;

    void Start()
    {
        ResolveSceneReferences();
        ApplyMissionDialogue();
        InitializePatientViewState();
        SetTransitionOverlayAlpha(0f);

        if (lines == null || lines.Length == 0)
        {
            SetDialogueVisible(false);
            GameplayPause.SetPaused(false);
            return;
        }

        GameplayPause.SetPaused(true);
        SetDialogueVisible(true);
        ShowCurrentLine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || (CanAcceptAdvanceTap() && WasAdvanceTap()))
        {
            NextLine();
        }
    }

    void ApplyMissionDialogue()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        if (mission == null ||
            mission.surgeryLines == null ||
            mission.surgeryLines.Length == 0)
        {
            return;
        }

        lines = mission.surgeryLines;
    }

    void OnEnable()
    {
        SpellController.SpellCastSucceeded += HandleSpellCastSucceeded;
        SpellController.SpellSelected += HandleSpellSelected;
        CutWound.WoundCauterised += HandleWoundCauterised;
    }

    void OnDisable()
    {
        SpellController.SpellCastSucceeded -= HandleSpellCastSucceeded;
        SpellController.SpellSelected -= HandleSpellSelected;
        CutWound.WoundCauterised -= HandleWoundCauterised;
    }

    public void NextLine()
    {
        if (lastAdvanceFrame == Time.frameCount)
        {
            return;
        }

        lastAdvanceFrame = Time.frameCount;

        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (transitionRoutine != null ||
            isWaitingForSpellSelect ||
            isWaitingForSpellCast ||
            isWaitingForWoundsCleared)
        {
            return;
        }

        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

        ContinueFromCurrentLine();
    }

    bool WasAdvanceTap()
    {
        if (Input.GetMouseButtonUp(0))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                return true;
            }
        }

        return false;
    }

    bool CanAcceptAdvanceTap()
    {
        return GameplayPause.IsPaused &&
               dialogueRoot != null &&
               dialogueRoot.activeInHierarchy &&
               (nextButtonRoot == null || nextButtonRoot.activeInHierarchy) &&
               transitionRoutine == null &&
               !isWaitingForSpellSelect &&
               !isWaitingForSpellCast &&
               !isWaitingForWoundsCleared;
    }

    void ShowCurrentLine()
    {
        SetDialogueVisible(true);

        if (speakerText == null || dialogueText == null)
        {
            return;
        }

        if (TryEnterPreShowWait())
        {
            return;
        }

        isWaitingForSpellSelect = false;
        isWaitingForSpellCast = false;
        isWaitingForWoundsCleared = false;
        isWaitingBeforeShowingLine = false;
        hasMatchedSpellSelection = false;
        hasMatchedSpellCast = false;
        SetNextButtonVisible(true);
        GameplayPause.SetPaused(true);
        lastAdvanceFrame = Time.frameCount;
        speakerText.text = lines[currentLineIndex].speaker;
        StartTypewriter(lines[currentLineIndex].text);
    }

    bool TryEnterPreShowWait()
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            return false;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];
        if (!currentLine.waitBeforeShowingLine)
        {
            return false;
        }

        ResolveSceneReferences();
        isWaitingForSpellSelect = currentLine.waitForSpellSelect;
        isWaitingForSpellCast = currentLine.waitForSpellCast;
        isWaitingForWoundsCleared = currentLine.waitForWoundsCleared;
        isWaitingBeforeShowingLine = true;
        hasMatchedSpellSelection = false;
        hasMatchedSpellCast = false;

        if (isWaitingForSpellSelect && spellController != null)
        {
            spellController.SetSelectedSpell(SpellController.SpellType.None);
        }

        SetNextButtonVisible(false);
        GameplayPause.SetPaused(false);
        TryContinueAfterWait();
        return true;
    }

    void StartTypewriter(string lineText)
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        typewriterRoutine = StartCoroutine(TypeLine(lineText));
    }

    IEnumerator TypeLine(string lineText)
    {
        isTyping = true;
        dialogueText.text = string.Empty;

        for (int i = 0; i < lineText.Length; i++)
        {
            dialogueText.text += lineText[i];

            if (audioSource != null &&
                voiceTestClip != null &&
                !char.IsWhiteSpace(lineText[i]))
            {
                audioSource.PlayOneShot(voiceTestClip);
            }

            yield return new WaitForSecondsRealtime(characterRevealInterval);
        }

        isTyping = false;
        typewriterRoutine = null;
        ApplyCurrentLineWaitState();
    }

    void FinishCurrentLineImmediately()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        isTyping = false;
        dialogueText.text = lines[currentLineIndex].text;
        ApplyCurrentLineWaitState();
    }

    void EndDialogue()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isWaitingForSpellSelect = false;
        isWaitingForSpellCast = false;
        isWaitingForWoundsCleared = false;
        isWaitingBeforeShowingLine = false;
        hasMatchedSpellSelection = false;
        hasMatchedSpellCast = false;
        GameplayPause.SetPaused(false);
        SetDialogueVisible(false);
    }

    void SetDialogueVisible(bool visible)
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(visible);
        }
    }

    void SetNextButtonVisible(bool visible)
    {
        if (nextButtonRoot != null)
        {
            nextButtonRoot.SetActive(visible);
        }
    }

    void ApplyCurrentLineWaitState()
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        if (lines[currentLineIndex].waitForSpellSelect)
        {
            isWaitingForSpellSelect = true;
            hasMatchedSpellSelection = false;
            if (spellController != null)
            {
                spellController.SetSelectedSpell(SpellController.SpellType.None);
            }
        }

        if (lines[currentLineIndex].waitForSpellCast)
        {
            isWaitingForSpellCast = true;
            hasMatchedSpellCast = false;
        }

        if (lines[currentLineIndex].waitForWoundsCleared)
        {
            isWaitingForWoundsCleared = true;
        }

        if (lines[currentLineIndex].missionCompleteFlag)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(CompleteMissionAfterLine());
            return;
        }

        if (isWaitingForSpellSelect || isWaitingForSpellCast || isWaitingForWoundsCleared)
        {
            ClearDialogueForOperationWait();
            SetNextButtonVisible(false);
            GameplayPause.SetPaused(false);
            TryContinueAfterWait();
        }
    }

    void ClearDialogueForOperationWait()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        SetDialogueVisible(false);
    }

    void HandleSpellCastSucceeded(SpellController.SpellType spellType)
    {
        if (!isWaitingForSpellCast || lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];
        if (currentLine.requiredSpell != SpellController.SpellType.None &&
            currentLine.requiredSpell != spellType)
        {
            return;
        }

        hasMatchedSpellCast = true;
        TryContinueAfterWait();
    }

    void HandleSpellSelected(SpellController.SpellType spellType)
    {
        if (!isWaitingForSpellSelect || lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];
        if (currentLine.requiredSpell != SpellController.SpellType.None &&
            currentLine.requiredSpell != spellType)
        {
            return;
        }

        hasMatchedSpellSelection = true;
        TryContinueAfterWait();
    }

    void HandleWoundCauterised(CutWound wound)
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];
        if (isWaitingForSpellCast &&
            currentLine.requiredSpell == SpellController.SpellType.Fire)
        {
            hasMatchedSpellCast = true;
        }

        TryContinueAfterWait();
    }

    bool AreRequiredWoundsCleared(PatientDialogueLine line)
    {
        ResolveSceneReferences();

        if (patientWounds == null)
        {
            return false;
        }

        return patientWounds.GetOpenWoundCount(line.requiredWoundLocation, line.requiredSpawnAreaId) == 0;
    }

    void TryContinueAfterWait()
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];
        ResolveSceneReferences();
        if (!string.IsNullOrWhiteSpace(currentLine.activateSpawnAreaId) && patientWounds != null)
        {
            patientWounds.SetWoundsActiveBySpawnArea(currentLine.activateSpawnAreaId, true);
        }

        bool spellSelectSatisfied = !currentLine.waitForSpellSelect || hasMatchedSpellSelection;
        bool spellCastSatisfied = !currentLine.waitForSpellCast || hasMatchedSpellCast;
        bool woundsSatisfied = !currentLine.waitForWoundsCleared || AreRequiredWoundsCleared(currentLine);

        if (!spellSelectSatisfied || !spellCastSatisfied || !woundsSatisfied)
        {
            return;
        }

        bool shouldShowCurrentLineAfterWait = isWaitingBeforeShowingLine;
        isWaitingForSpellSelect = false;
        isWaitingForSpellCast = false;
        isWaitingForWoundsCleared = false;
        isWaitingBeforeShowingLine = false;
        hasMatchedSpellSelection = false;
        hasMatchedSpellCast = false;

        if (shouldShowCurrentLineAfterWait)
        {
            SetDialogueVisible(true);
            ShowCurrentLine();
            return;
        }

        ContinueFromCurrentLine();
    }

    void ContinueAfterWait()
    {
        GameplayPause.SetPaused(true);
        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ContinueFromCurrentLine()
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        if (ShouldTransitionToInside(lines[currentLineIndex]))
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(TransitionToInsideAndContinue());
            return;
        }

        if (ShouldTransitionToPart(lines[currentLineIndex]))
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(TransitionToPartAndContinue());
            return;
        }

        ApplyLineAdvanceActions(lines[currentLineIndex]);

        ContinueAfterWait();
    }

    bool ShouldTransitionToInside(PatientDialogueLine line)
    {
        return line != null &&
               line.transitionToInside &&
               !hasTransitionedInside &&
               outsidePatientRoot != null &&
               insidePatientRoot != null &&
               transitionFadeOverlay != null;
    }

    bool ShouldTransitionToPart(PatientDialogueLine line)
    {
        return line != null &&
               line.transitionToPart &&
               !hasTransitionedToPart &&
               partPatientRoot != null &&
               transitionFadeOverlay != null &&
               currentPatientRoot != null &&
               currentPatientRoot != partPatientRoot;
    }

    IEnumerator TransitionToInsideAndContinue()
    {
        yield return TransitionToRootAndContinue(insidePatientRoot, CutWound.WoundLocation.Inside, () =>
        {
            hasTransitionedInside = true;
        });
    }

    IEnumerator TransitionToPartAndContinue()
    {
        yield return TransitionToRootAndContinue(partPatientRoot, CutWound.WoundLocation.Part, () =>
        {
            hasTransitionedToPart = true;
        });
    }

    IEnumerator TransitionToRootAndContinue(GameObject targetRoot, CutWound.WoundLocation targetWoundLocation, System.Action onMidTransition)
    {
        GameplayPause.SetPaused(true);
        SetNextButtonVisible(false);

        yield return FadeOverlay(0f, 1f);

        if (targetRoot != null)
        {
            targetRoot.SetActive(true);
        }

        if (currentPatientRoot != null && currentPatientRoot != targetRoot)
        {
            currentPatientRoot.SetActive(false);
        }

        currentPatientRoot = targetRoot;
        onMidTransition?.Invoke();

        if (transitionMidpointHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(transitionMidpointHoldDuration);
        }

        yield return FadeOverlay(1f, 0f);

        PatientDialogueLine currentLine = lines != null && currentLineIndex < lines.Length
            ? lines[currentLineIndex]
            : null;

        if (currentLine != null && !string.IsNullOrWhiteSpace(currentLine.focusSpawnAreaId))
        {
            ApplyLineAdvanceActions(currentLine);
        }
        else
        {
            MoveCameraToWoundLocation(targetWoundLocation, targetRoot);
        }

        transitionRoutine = null;
        ContinueAfterWait();
    }

    void ApplyLineAdvanceActions(PatientDialogueLine line)
    {
        if (line == null)
        {
            return;
        }

        ResolveSceneReferences();

        if (!string.IsNullOrWhiteSpace(line.activateSpawnAreaId) && patientWounds != null)
        {
            patientWounds.SetWoundsActiveBySpawnArea(line.activateSpawnAreaId, true);
        }

        if (line.triggerEmergencyTimeDilation)
        {
            EmergencyTimeDilationEffect.Activate();
        }

        if (line.triggerEmergencyImpact)
        {
            EmergencyTimeDilationEffect.PlayImpact();
        }

        if (!string.IsNullOrWhiteSpace(line.focusSpawnAreaId))
        {
            MoveCameraToSpawnArea(line.focusSpawnAreaId);
        }
    }

    void MoveCameraToWoundLocation(CutWound.WoundLocation woundLocation, GameObject targetRoot)
    {
        SurgeryCameraTurnIn surgeryCamera = Camera.main != null
            ? Camera.main.GetComponent<SurgeryCameraTurnIn>()
            : FindAnyObjectByType<SurgeryCameraTurnIn>();

        if (surgeryCamera == null)
        {
            return;
        }

        Transform focusRoot = targetRoot != null ? targetRoot.transform : null;
        surgeryCamera.FocusForWoundLocation(woundLocation, focusRoot);
    }

    void MoveCameraToSpawnArea(string spawnAreaId)
    {
        SurgeryCameraTurnIn surgeryCamera = Camera.main != null
            ? Camera.main.GetComponent<SurgeryCameraTurnIn>()
            : FindAnyObjectByType<SurgeryCameraTurnIn>();

        if (surgeryCamera == null)
        {
            return;
        }

        Transform focusRoot = currentPatientRoot != null ? currentPatientRoot.transform : null;
        surgeryCamera.FocusForSpawnArea(spawnAreaId, focusRoot);
    }

    IEnumerator CompleteMissionAfterLine()
    {
        GameplayPause.SetPaused(true);
        SetNextButtonVisible(false);
        SetDialogueVisible(false);
        yield return null;

        transitionRoutine = null;

        if (surgeryEndController == null)
        {
            surgeryEndController = FindAnyObjectByType<SurgeryEndController>();
        }

        if (surgeryEndController != null)
        {
            EmergencyTimeDilationEffect.Deactivate();
            surgeryEndController.ShowMissionComplete();
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator FadeOverlay(float fromAlpha, float toAlpha)
    {
        if (transitionFadeOverlay == null)
        {
            yield break;
        }

        transitionFadeOverlay.gameObject.SetActive(true);
        transitionFadeOverlay.raycastTarget = false;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTransitionOverlayAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetTransitionOverlayAlpha(toAlpha);

        if (toAlpha <= 0f)
        {
            transitionFadeOverlay.gameObject.SetActive(false);
        }
    }

    void SetTransitionOverlayAlpha(float alpha)
    {
        if (transitionFadeOverlay == null)
        {
            return;
        }

        Color color = transitionFadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        transitionFadeOverlay.color = color;

        if (color.a <= 0f)
        {
            transitionFadeOverlay.gameObject.SetActive(false);
        }
        else if (!transitionFadeOverlay.gameObject.activeSelf)
        {
            transitionFadeOverlay.gameObject.SetActive(true);
        }
    }

    void ResolveSceneReferences()
    {
        if (patientWounds == null)
        {
            if (spellController != null && spellController.patient != null)
            {
                patientWounds = spellController.patient.GetComponent<PatientWounds>();
            }

            if (patientWounds == null)
            {
                patientWounds = FindAnyObjectByType<PatientWounds>();
            }
        }

        if (surgeryEndController == null)
        {
            surgeryEndController = FindAnyObjectByType<SurgeryEndController>();
        }
    }

    void InitializePatientViewState()
    {
        if (!hasTransitionedInside)
        {
            if (outsidePatientRoot != null)
            {
                outsidePatientRoot.SetActive(true);
                currentPatientRoot = outsidePatientRoot;
            }

            if (insidePatientRoot != null)
            {
                insidePatientRoot.SetActive(false);
            }

            if (partPatientRoot != null)
            {
                partPatientRoot.SetActive(false);
            }
        }
    }
}
