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
    public SpellController.SpellType requiredSpell = SpellController.SpellType.None;
}

public class PatientDialogueController : MonoBehaviour
{
    [SerializeField] private SpellController spellController;
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

    void Start()
    {
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
    }

    void OnDisable()
    {
        SpellController.SpellCastSucceeded -= HandleSpellCastSucceeded;
        SpellController.SpellSelected -= HandleSpellSelected;
    }

    public void NextLine()
    {
        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (isWaitingForSpellSelect || isWaitingForSpellCast)
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

        isWaitingForSpellSelect = false;
        isWaitingForSpellCast = false;
        SetNextButtonVisible(true);
        GameplayPause.SetPaused(true);
        speakerText.text = lines[currentLineIndex].speaker;
        StartTypewriter(lines[currentLineIndex].text);
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
            if (spellController != null)
            {
                spellController.SetSelectedSpell(SpellController.SpellType.None);
            }
            SetNextButtonVisible(false);
            GameplayPause.SetPaused(false);
            return;
        }

        if (lines[currentLineIndex].waitForSpellCast)
        {
            isWaitingForSpellCast = true;
            SetNextButtonVisible(false);
            GameplayPause.SetPaused(false);
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

        isWaitingForSpellCast = false;
        GameplayPause.SetPaused(true);
        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
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

        isWaitingForSpellSelect = false;
        GameplayPause.SetPaused(true);
        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }
}
