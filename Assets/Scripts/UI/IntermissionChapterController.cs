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
    [SerializeField] private Color preOpBackgroundColor = new Color(0.05f, 0.035f, 0.028f, 1f);
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private string backgroundSpriteResource = "Sprites/TavernIntermissionBackground";
    [SerializeField] private Color dialoguePanelColor = new Color(1f, 1f, 1f, 0.392f);
    [SerializeField] private float sceneFadeDuration = 0.45f;
    [SerializeField] private float titleCardDelay = 0.15f;
    [SerializeField] private float titleCardSlideDuration = 0.75f;
    [SerializeField] private float titleCardHoldDuration = 1.05f;
    [SerializeField] private float titleCardFadeDuration = 0.55f;

    private DialogueLine[] lines;
    private int currentLineIndex;
    private Coroutine typewriterRoutine;
    private Coroutine titleCardRoutine;
    private bool isTyping;
    private int lastAdvanceFrame = -1;
    private CanvasGroup sceneFadeGroup;
    private bool isCompleting;
    private RectTransform canvasRoot;
    private GameObject dialoguePanelRoot;

    void Start()
    {
        if (speakerText == null || dialogueText == null)
        {
            BuildDefaultLayout();
        }
        else
        {
            EnsureBackgroundOnExistingCanvas();
        }

        MissionData mission = MissionFlowState.CurrentMission;
        lines = mission != null && mission.intermissionLines != null && mission.intermissionLines.Length > 0
            ? mission.intermissionLines
            : fallbackLines;

        SetDialoguePanelVisible(false);

        if (sceneFadeGroup != null)
        {
            StartCoroutine(FadeSceneOverlay(1f, 0f));
        }

        titleCardRoutine = StartCoroutine(PlayTitleCardThenDialogue(mission));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || WasAdvanceTap())
        {
            NextLine();
        }
    }

    public void NextLine()
    {
        if (lastAdvanceFrame == Time.frameCount)
        {
            return;
        }

        lastAdvanceFrame = Time.frameCount;

        if (titleCardRoutine != null)
        {
            return;
        }

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
        if (isCompleting)
        {
            return;
        }

        isCompleting = true;
        StartCoroutine(CompleteIntermissionAfterFade());
    }

    IEnumerator CompleteIntermissionAfterFade()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        MissionFlowState.MarkCompleted(mission);
        yield return FadeSceneOverlay(sceneFadeGroup != null ? sceneFadeGroup.alpha : 0f, 1f);
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
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = canvasObject.AddComponent<Image>();
        ConfigureBackgroundImage(background);

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform dialoguePanel = CreatePanel("DialoguePanel", root, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
        dialoguePanel.anchoredPosition = new Vector2(0f, 200f);
        dialoguePanel.sizeDelta = new Vector2(-240f, 320f);
        Image panelImage = dialoguePanel.gameObject.AddComponent<Image>();
        panelImage.color = dialoguePanelColor;
        dialoguePanelRoot = dialoguePanel.gameObject;
        canvasRoot = root;

        speakerText = CreateText("SpeakerText", root, string.Empty, 24, TextAlignmentOptions.Left);
        speakerText.rectTransform.anchorMin = new Vector2(0f, 0f);
        speakerText.rectTransform.anchorMax = new Vector2(1f, 0f);
        speakerText.rectTransform.anchoredPosition = new Vector2(0f, 380f);
        speakerText.rectTransform.sizeDelta = new Vector2(-240f, 48f);
        speakerText.color = Color.white;
        speakerText.fontStyle = FontStyles.Normal;

        dialogueText = CreateText("DialogueText", dialoguePanel, string.Empty, 36, TextAlignmentOptions.TopLeft);
        dialogueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        dialogueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        dialogueText.rectTransform.offsetMin = new Vector2(48f, 44f);
        dialogueText.rectTransform.offsetMax = new Vector2(-340f, -56f);
        dialogueText.color = Color.black;
        dialogueText.fontStyle = FontStyles.Normal;

        Button nextButton = CreateButton("NextButton", dialoguePanel, "Next", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform nextButtonRect = nextButton.transform as RectTransform;
        nextButtonRect.anchorMin = new Vector2(1f, 0.5f);
        nextButtonRect.anchorMax = new Vector2(1f, 0.5f);
        nextButtonRect.anchoredPosition = new Vector2(-150f, 0f);
        nextButtonRect.sizeDelta = new Vector2(160f, 160f);
        nextButton.onClick.AddListener(NextLine);

        CreateSceneFade(root);
    }

    void EnsureBackgroundOnExistingCanvas()
    {
        Canvas canvas = dialogueText != null ? dialogueText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        canvasRoot = canvasRect;
        if (dialogueText != null &&
            dialogueText.transform.parent != null &&
            dialogueText.transform.parent != canvas.transform)
        {
            dialoguePanelRoot = dialogueText.transform.parent.gameObject;
        }

        RectTransform backgroundRect = FindDirectChild(canvasRect, "IntermissionBackground");
        Image backgroundImage;
        if (backgroundRect == null)
        {
            GameObject backgroundObject = new GameObject("IntermissionBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(canvasRect, false);
            backgroundImage = backgroundObject.GetComponent<Image>();
        }
        else
        {
            backgroundImage = backgroundRect.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
            }
        }

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        ConfigureBackgroundImage(backgroundImage);
        backgroundRect.SetAsFirstSibling();
    }

    IEnumerator PlayTitleCardThenDialogue(MissionData mission)
    {
        if (titleCardDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(titleCardDelay);
        }

        Canvas canvas = dialogueText != null ? dialogueText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        RectTransform root = canvasRoot != null ? canvasRoot : (canvas != null ? canvas.transform as RectTransform : null);
        if (root == null)
        {
            SetDialoguePanelVisible(true);
            ShowCurrentLine();
            yield break;
        }

        string title = GetMissionTitleCardText(mission);
        GameObject titleObject = new GameObject("VNTitleCard", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.SetParent(root, false);
        if (sceneFadeGroup != null)
        {
            sceneFadeGroup.transform.SetAsLastSibling();
        }

        titleRect.anchorMin = new Vector2(0f, 0.62f);
        titleRect.anchorMax = new Vector2(0f, 0.62f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.sizeDelta = new Vector2(1680f, 170f);

        CanvasGroup titleGroup = titleObject.GetComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.interactable = false;
        titleGroup.blocksRaycasts = false;

        TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 64f;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = new Color(0.96f, 0.88f, 0.68f, 1f);
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        Vector2 startPosition = new Vector2(-1380f, 0f);
        Vector2 endPosition = new Vector2(140f, 0f);
        yield return SlideTitleCard(titleRect, titleGroup, startPosition, endPosition);

        if (titleCardHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(titleCardHoldDuration);
        }

        yield return FadeTitleCardOut(titleGroup, titleRect, endPosition);

        if (titleObject != null)
        {
            Destroy(titleObject);
        }

        titleCardRoutine = null;
        SetDialoguePanelVisible(true);
        ShowCurrentLine();
    }

    IEnumerator SlideTitleCard(RectTransform titleRect, CanvasGroup titleGroup, Vector2 startPosition, Vector2 endPosition)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, titleCardSlideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            titleRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            titleGroup.alpha = t;
            yield return null;
        }

        titleRect.anchoredPosition = endPosition;
        titleGroup.alpha = 1f;
    }

    IEnumerator FadeTitleCardOut(CanvasGroup titleGroup, RectTransform titleRect, Vector2 startPosition)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, titleCardFadeDuration);
        Vector2 endPosition = startPosition + new Vector2(80f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            titleRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            titleGroup.alpha = 1f - t;
            yield return null;
        }
    }

    string GetMissionTitleCardText(MissionData mission)
    {
        if (mission == null)
        {
            return "P-0 | Prologue";
        }

        string code = !string.IsNullOrWhiteSpace(mission.episodeCode) ? mission.episodeCode : mission.missionId;
        string title = !string.IsNullOrWhiteSpace(mission.displayName) ? mission.displayName : "Untitled";
        return code + " | " + title;
    }

    void SetDialoguePanelVisible(bool visible)
    {
        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(visible);
        }

        if (dialoguePanelRoot != null)
        {
            dialoguePanelRoot.SetActive(visible);
        }
        else if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(visible);
        }
    }

    void ConfigureBackgroundImage(Image backgroundImage)
    {
        if (backgroundImage == null)
        {
            return;
        }

        Sprite sprite = ResolveBackgroundSprite();
        backgroundImage.sprite = sprite;
        backgroundImage.color = sprite != null ? Color.white : preOpBackgroundColor;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = false;
    }

    Sprite ResolveBackgroundSprite()
    {
        if (backgroundSprite != null)
        {
            return backgroundSprite;
        }

        if (string.IsNullOrWhiteSpace(backgroundSpriteResource))
        {
            return null;
        }

        backgroundSprite = LoadBackgroundSprite(backgroundSpriteResource);
        if (backgroundSprite != null)
        {
            return backgroundSprite;
        }

        backgroundSprite = LoadBackgroundSprite("Sprites/IntermissionBackground");
        if (backgroundSprite != null)
        {
            return backgroundSprite;
        }

        backgroundSprite = LoadBackgroundSprite("Sprites/PrologueBackground");
        if (backgroundSprite != null)
        {
            return backgroundSprite;
        }

        Debug.LogWarning("VN background missing. Import the tavern image as Assets/Resources/Sprites/TavernIntermissionBackground.png.");
        return null;
    }

    Sprite LoadBackgroundSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    RectTransform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child as RectTransform;
            }
        }

        return null;
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

        TextMeshProUGUI label = CreateText("Label", buttonRect, labelText, 24, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
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

    void CreateSceneFade(RectTransform root)
    {
        RectTransform fadeRect = CreatePanel("SceneFade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fadeImage = fadeRect.gameObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        sceneFadeGroup = fadeRect.gameObject.AddComponent<CanvasGroup>();
        sceneFadeGroup.alpha = 1f;
        sceneFadeGroup.interactable = false;
        sceneFadeGroup.blocksRaycasts = true;
        fadeRect.SetAsLastSibling();
    }

    IEnumerator FadeSceneOverlay(float fromAlpha, float toAlpha)
    {
        if (sceneFadeGroup == null)
        {
            yield break;
        }

        sceneFadeGroup.blocksRaycasts = toAlpha > fromAlpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, sceneFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            sceneFadeGroup.alpha = Mathf.SmoothStep(fromAlpha, toAlpha, t);
            yield return null;
        }

        sceneFadeGroup.alpha = toAlpha;
        sceneFadeGroup.blocksRaycasts = toAlpha > 0f;
    }
}
