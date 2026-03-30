using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;
}

public class PreOpDialogueController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string nextSceneName = "PatientScene";
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private float characterRevealInterval = 0.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceTestClip;

    private int currentLineIndex;
    private Coroutine typewriterRoutine;
    private bool isTyping;

    void Start()
    {
        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (lines == null || lines.Length == 0)
        {
            SceneManager.LoadScene(nextSceneName);
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
            SceneManager.LoadScene(nextSceneName);
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

        if (lines == null || lines.Length == 0)
        {
            speakerText.text = string.Empty;
            dialogueText.text = string.Empty;
            return;
        }

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

            yield return new WaitForSeconds(characterRevealInterval);
        }

        isTyping = false;
        typewriterRoutine = null;
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
    }
}
