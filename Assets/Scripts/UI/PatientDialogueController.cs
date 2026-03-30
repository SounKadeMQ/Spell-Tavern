using System.Collections;
using TMPro;
using UnityEngine;

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
    public SpellController.SpellType requiredSpell = SpellController.SpellType.None;
    public CutWound.WoundLocation requiredWoundLocation = CutWound.WoundLocation.Outside;
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

    private int currentLineIndex;
    private Coroutine typewriterRoutine;
    private bool isTyping;
    private bool isWaitingForSpellSelect;
    private bool isWaitingForSpellCast;
    private bool isWaitingForWoundsCleared;
    private bool isWaitingBeforeShowingLine;
    private bool hasMatchedSpellSelection;
    private bool hasMatchedSpellCast;

    void Start()
    {
        ResolveSceneReferences();

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
        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (isWaitingForSpellSelect || isWaitingForSpellCast || isWaitingForWoundsCleared)
        {
            return;
        }

        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
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

        if (isWaitingForSpellSelect || isWaitingForSpellCast || isWaitingForWoundsCleared)
        {
            SetNextButtonVisible(false);
            GameplayPause.SetPaused(false);
            TryContinueAfterWait();
        }
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
        if (!isWaitingForWoundsCleared || lines == null || currentLineIndex >= lines.Length)
        {
            return;
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

        return patientWounds.GetOpenWoundCount(line.requiredWoundLocation) == 0;
    }

    void TryContinueAfterWait()
    {
        if (lines == null || currentLineIndex >= lines.Length)
        {
            return;
        }

        PatientDialogueLine currentLine = lines[currentLineIndex];

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
            ShowCurrentLine();
            return;
        }

        ContinueAfterWait();
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
    }
}
