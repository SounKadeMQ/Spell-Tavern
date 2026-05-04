using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntermissionChapterController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string missionSelectSceneName = "ChapterSelect";
    [SerializeField] private DialogueLine[] fallbackLines;
    [SerializeField] private float characterRevealInterval = 0.05f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceClip;

    private DialogueLine[] lines;
    private int currentLineIndex;
    private Coroutine typewriterRoutine;
    private bool isTyping;

    void Start()
    {
        if (speakerText == null || dialogueText == null)
        {
            BuildDefaultLayout();
        }

        MissionData mission = MissionFlowState.CurrentMission;
        lines = mission != null && mission.intermissionLines != null && mission.intermissionLines.Length > 0
            ? mission.intermissionLines
            : fallbackLines;

        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (lines == null || lines.Length == 0)
        {
            CompleteIntermission();
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
            CompleteIntermission();
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
                voiceClip != null &&
                !char.IsWhiteSpace(lineText[i]))
            {
                audioSource.PlayOneShot(voiceClip);
            }

            yield return new WaitForSecondsRealtime(characterRevealInterval);
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

    void CompleteIntermission()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        MissionFlowState.MarkCompleted(mission);
        SceneManager.LoadScene(missionSelectSceneName);
    }

    void BuildDefaultLayout()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("IntermissionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.055f, 0.065f, 1f);

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform dialoguePanel = CreatePanel("DialoguePanel", root, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
        Image panelImage = dialoguePanel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.11f, 0.09f, 0.08f, 0.95f);

        speakerText = CreateText("SpeakerText", dialoguePanel, string.Empty, 24, TextAlignmentOptions.Left);
        speakerText.rectTransform.anchorMin = new Vector2(0f, 0.7f);
        speakerText.rectTransform.anchorMax = new Vector2(0.7f, 1f);
        speakerText.rectTransform.offsetMin = new Vector2(24f, 0f);
        speakerText.rectTransform.offsetMax = new Vector2(-24f, -12f);

        dialogueText = CreateText("DialogueText", dialoguePanel, string.Empty, 22, TextAlignmentOptions.TopLeft);
        dialogueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        dialogueText.rectTransform.anchorMax = new Vector2(1f, 0.72f);
        dialogueText.rectTransform.offsetMin = new Vector2(24f, 20f);
        dialogueText.rectTransform.offsetMax = new Vector2(-24f, 0f);

        Button nextButton = CreateButton("NextButton", dialoguePanel, "Next", new Vector2(0.78f, 0.72f), new Vector2(0.96f, 0.94f));
        nextButton.onClick.AddListener(NextLine);
    }

    RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        return rectTransform;
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        return label;
    }

    Button CreateButton(string objectName, Transform parent, string labelText, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform buttonRect = CreatePanel(objectName, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.55f, 0.16f, 0.08f, 1f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText("Label", buttonRect, labelText, 20, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
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
