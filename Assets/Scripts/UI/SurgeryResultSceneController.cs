using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SurgeryResultSceneController : MonoBehaviour
{
    [SerializeField] private string missionSelectSceneName = "ChapterSelect";
    [SerializeField] private Color backgroundTopColor = new Color(0.18f, 0.08f, 0.12f, 1f);
    [SerializeField] private Color backgroundBottomColor = new Color(0.95f, 0.86f, 0.7f, 1f);
    [SerializeField] private Color panelColor = new Color(1f, 0.95f, 0.82f, 0.92f);
    [SerializeField] private Color inkColor = new Color(0.08f, 0.045f, 0.035f, 1f);
    [SerializeField] private Color accentColor = new Color(0.9f, 0.55f, 0.32f, 1f);
    [SerializeField] private AudioSource typewriterAudioSource;
    [SerializeField] private AudioClip typewriterClip;
    [SerializeField] private float typewriterVolume = 0.55f;

    private TMP_FontAsset resultFont;

    void Start()
    {
        if (!SurgeryResultState.HasResult)
        {
            SceneManager.LoadScene(missionSelectSceneName);
            return;
        }

        EnsureSceneCamera();
        EnsureEventSystem();
        BuildResultScreen();
    }

    void BuildResultScreen()
    {
        Canvas canvas = CreateCanvas();
        RectTransform root = canvas.transform as RectTransform;
        CanvasGroup screenGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        screenGroup.alpha = 0f;
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;
        EnsureTypewriterAudio(canvas.gameObject);

        Image background = canvas.gameObject.AddComponent<Image>();
        background.sprite = CreateResultBackgroundSprite(1024, 576);
        background.color = Color.white;

        CanvasGroup introGroup = CreateGroup("IntroGroup", root);
        CanvasGroup scoreGroup = CreateGroup("ScoreGroup", root);
        CanvasGroup finalGroup = CreateGroup("FinalGroup", root);

        TextMeshProUGUI introTitle = CreateIntroScreen(introGroup.transform as RectTransform);
        ScoreScreenElements scoreElements = CreateScoreScreen(scoreGroup.transform as RectTransform);
        FinalScreenElements finalElements = CreateFinalScreen(finalGroup.transform as RectTransform);

        SetGroupVisible(scoreGroup, false);
        SetGroupVisible(finalGroup, false);
        finalGroup.interactable = false;
        finalGroup.blocksRaycasts = false;

        StartCoroutine(PlayResultSequence(screenGroup, introGroup, introTitle, scoreGroup, scoreElements, finalGroup, finalElements));
    }

    Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("SurgeryResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    TextMeshProUGUI CreateIntroScreen(RectTransform root)
    {
        TextMeshProUGUI title = CreateTitle(root);
        title.text = string.Empty;
        return title;
    }

    ScoreScreenElements CreateScoreScreen(RectTransform root)
    {
        CreateTopRule(root);

        RectTransform panel = CreatePanel("ResultPanel", root, new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.69f), Vector2.zero, Vector2.zero);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;

        ScoreScreenElements elements = new ScoreScreenElements
        {
            stageRow = AddScoreRow(panel, "STAGE SCORE", SurgeryResultState.StageScore, 0.78f),
            vitalRow = AddScoreRow(panel, "VITAL BONUS", SurgeryResultState.VitalBonus, 0.6f),
            timeRow = AddScoreRow(panel, "TIME BONUS", SurgeryResultState.TimeBonus, 0.42f),
            specialRow = AddScoreRow(panel, "SPECIAL BONUS", SurgeryResultState.SpecialBonus, 0.24f),
            specialList = CreateText("SpecialList", panel, string.Empty, 24f, TextAlignmentOptions.TopLeft, inkColor)
        };

        elements.specialList.rectTransform.anchorMin = new Vector2(0.06f, 0.02f);
        elements.specialList.rectTransform.anchorMax = new Vector2(0.9f, 0.22f);
        elements.specialList.rectTransform.offsetMin = Vector2.zero;
        elements.specialList.rectTransform.offsetMax = Vector2.zero;

        ClearRow(elements.stageRow);
        ClearRow(elements.vitalRow);
        ClearRow(elements.timeRow);
        ClearRow(elements.specialRow);
        return elements;
    }

    FinalScreenElements CreateFinalScreen(RectTransform root)
    {
        CreateTopRule(root);
        CreateTitle(root);

        TextMeshProUGUI operation = CreateText("OperationLabel", root, "Operation", 76f, TextAlignmentOptions.Center, new Color(0.16f, 0.1f, 0.08f, 0.24f));
        operation.rectTransform.anchorMin = new Vector2(0.06f, 0.38f);
        operation.rectTransform.anchorMax = new Vector2(0.94f, 0.58f);
        operation.rectTransform.offsetMin = Vector2.zero;
        operation.rectTransform.offsetMax = Vector2.zero;

        string rankPrefix = GetRankPrefix();
        TextMeshProUGUI rank = CreateText("RankStamp", root, "RANK " + rankPrefix, 82f, TextAlignmentOptions.Center, GetRankColor(rankPrefix));
        rank.rectTransform.anchorMin = new Vector2(0.2f, 0.39f);
        rank.rectTransform.anchorMax = new Vector2(0.8f, 0.54f);
        rank.rectTransform.offsetMin = Vector2.zero;
        rank.rectTransform.offsetMax = Vector2.zero;
        Color hiddenRankColor = rank.color;
        hiddenRankColor.a = 0f;
        rank.color = hiddenRankColor;

        TextMeshProUGUI rankName = CreateText("RankName", root, GetRankName(), 42f, TextAlignmentOptions.Center, GetRankColor(rankPrefix));
        rankName.rectTransform.anchorMin = new Vector2(0.2f, 0.31f);
        rankName.rectTransform.anchorMax = new Vector2(0.8f, 0.39f);
        rankName.rectTransform.offsetMin = Vector2.zero;
        rankName.rectTransform.offsetMax = Vector2.zero;
        Color hiddenRankNameColor = rankName.color;
        hiddenRankNameColor.a = 0f;
        rankName.color = hiddenRankNameColor;

        TextMeshProUGUI total = CreateText("OperationScore", root, string.Empty, 48f, TextAlignmentOptions.Center, inkColor);
        total.rectTransform.anchorMin = new Vector2(0.06f, 0.2f);
        total.rectTransform.anchorMax = new Vector2(0.94f, 0.29f);
        total.rectTransform.offsetMin = Vector2.zero;
        total.rectTransform.offsetMax = Vector2.zero;

        CreateButtons(root);

        return new FinalScreenElements
        {
            rank = rank,
            rankName = rankName,
            operationScore = total
        };
    }

    void CreateTopRule(RectTransform root)
    {
        RectTransform strip = CreatePanel("HeaderStrip", root, new Vector2(0f, 0.84f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image stripImage = strip.gameObject.AddComponent<Image>();
        stripImage.color = new Color(0.12f, 0.05f, 0.07f, 0.82f);

        TextMeshProUGUI resultText = CreateText("ResultTitle", strip, "RESULT", 76f, TextAlignmentOptions.Right, new Color(0.98f, 0.88f, 0.72f, 1f));
        resultText.rectTransform.anchorMin = new Vector2(0f, 0f);
        resultText.rectTransform.anchorMax = new Vector2(0.96f, 1f);
        resultText.rectTransform.offsetMin = Vector2.zero;
        resultText.rectTransform.offsetMax = Vector2.zero;

        RectTransform rule = CreatePanel("Rule", root, new Vector2(0f, 0.835f), new Vector2(1f, 0.845f), Vector2.zero, Vector2.zero);
        Image ruleImage = rule.gameObject.AddComponent<Image>();
        ruleImage.color = accentColor;
    }

    TextMeshProUGUI CreateTitle(RectTransform root)
    {
        TextMeshProUGUI title = CreateText("OperationSuccessful", root, "Operation Successful", 52f, TextAlignmentOptions.Center, new Color(0.18f, 0.6f, 0.34f, 1f));
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.68f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        RectTransform ecg = CreatePanel("EcgLine", root, new Vector2(0.08f, 0.665f), new Vector2(0.92f, 0.672f), Vector2.zero, Vector2.zero);
        Image ecgImage = ecg.gameObject.AddComponent<Image>();
        ecgImage.color = new Color(0.24f, 0.9f, 0.42f, 0.95f);
        return title;
    }

    IEnumerator PlayResultSequence(CanvasGroup screenGroup, CanvasGroup introGroup, TextMeshProUGUI introTitle, CanvasGroup scoreGroup, ScoreScreenElements scoreElements, CanvasGroup finalGroup, FinalScreenElements finalElements)
    {
        yield return FadeGroup(screenGroup, 0f, 1f, 0.35f);
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;

        yield return TypeText(introTitle, "Operation Successful", 0.04f);
        yield return new WaitForSecondsRealtime(0.6f);
        yield return FadeGroup(introGroup, 1f, 0f, 0.35f);

        yield return FadeGroup(scoreGroup, 0f, 1f, 0.35f);
        yield return TypeScoreRow(scoreElements.stageRow, "STAGE SCORE", SurgeryResultState.StageScore);
        yield return TypeScoreRow(scoreElements.vitalRow, "VITAL BONUS", SurgeryResultState.VitalBonus);
        yield return TypeScoreRow(scoreElements.timeRow, "TIME BONUS", SurgeryResultState.TimeBonus);
        yield return TypeScoreRow(scoreElements.specialRow, "SPECIAL BONUS", SurgeryResultState.SpecialBonus);
        yield return TypeText(scoreElements.specialList, GetSpecialList(), 0.012f);
        yield return new WaitForSecondsRealtime(0.8f);
        yield return FadeGroup(scoreGroup, 1f, 0f, 0.35f);

        yield return FadeGroup(finalGroup, 0f, 1f, 0.35f);
        finalGroup.interactable = false;
        finalGroup.blocksRaycasts = false;
        yield return TypeText(finalElements.operationScore, "OPERATION SCORE  " + SurgeryResultState.OperationScore, 0.025f);
        yield return StampRank(finalElements.rank);
        yield return FadeRankName(finalElements.rankName);
        screenGroup.interactable = true;
        screenGroup.blocksRaycasts = true;
        finalGroup.interactable = true;
        finalGroup.blocksRaycasts = true;
    }

    IEnumerator TypeScoreRow(ScoreRow row, string label, int value)
    {
        yield return TypeText(row.label, label, 0.012f);
        yield return TypeText(row.value, value.ToString(), 0.018f);
        yield return new WaitForSecondsRealtime(0.06f);
    }

    IEnumerator TypeText(TextMeshProUGUI text, string value, float characterDelay)
    {
        text.text = string.Empty;
        for (int i = 0; i < value.Length; i++)
        {
            text.text += value[i];
            PlayTypewriterSound(value[i]);
            yield return new WaitForSecondsRealtime(characterDelay);
        }
    }

    void PlayTypewriterSound(char character)
    {
        if (char.IsWhiteSpace(character) || typewriterAudioSource == null || typewriterClip == null)
        {
            return;
        }

        typewriterAudioSource.PlayOneShot(typewriterClip, typewriterVolume);
    }

    IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        group.alpha = from;
        group.blocksRaycasts = false;
        group.interactable = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = to;
        group.blocksRaycasts = to > 0.95f;
        group.interactable = to > 0.95f;
    }

    IEnumerator StampRank(TextMeshProUGUI rank)
    {
        string rankPrefix = GetRankPrefix();
        RectTransform rect = rank.rectTransform;
        Vector3 baseScale = Vector3.one;
        Vector3 punchScale = Vector3.one * GetRankPunchScale(rankPrefix);
        Color color = GetRankColor(rankPrefix);
        color.a = 0f;
        rank.color = color;
        rect.localScale = punchScale;

        float elapsed = 0f;
        const float duration = 0.42f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Color visibleColor = color;
            visibleColor.a = Mathf.Lerp(0f, 0.86f, eased);
            rank.color = visibleColor;
            rect.localScale = Vector3.Lerp(punchScale, baseScale, eased);

            if (IsLowRank(rankPrefix))
            {
                rect.anchoredPosition = new Vector2(Mathf.Sin(t * 34f) * 4f, Mathf.Sin(t * 49f) * 2f);
            }

            yield return null;
        }

        Color finalColor = GetRankColor(rankPrefix);
        finalColor.a = 0.86f;
        rank.color = finalColor;
        rect.localScale = baseScale;
        rect.anchoredPosition = Vector2.zero;
    }

    IEnumerator FadeRankName(TextMeshProUGUI rankName)
    {
        Color color = rankName.color;
        color.a = 0f;
        rankName.color = color;

        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, 0.92f, Mathf.Clamp01(elapsed / duration));
            rankName.color = color;
            yield return null;
        }

        color.a = 0.92f;
        rankName.color = color;
    }

    void CreateButtons(RectTransform root)
    {
        Button nextButton = CreateButton(root, "NextButton", "Next", new Vector2(-260f, 38f), new Color(0.95f, 0.82f, 0.76f, 1f));
        nextButton.onClick.AddListener(Continue);
        nextButton.interactable = HasNextMission();

        Button episodeSelectButton = CreateButton(root, "EpisodeSelectButton", "Episode Select", new Vector2(0f, 38f), new Color(0.9f, 0.72f, 0.66f, 1f));
        episodeSelectButton.onClick.AddListener(GoToEpisodeSelect);

        Button retryButton = CreateButton(root, "RetryButton", "Retry", new Vector2(260f, 38f), new Color(0.5f, 0.08f, 0.04f, 1f));
        retryButton.onClick.AddListener(Retry);
    }

    ScoreRow AddScoreRow(RectTransform parent, string label, int value, float y)
    {
        TextMeshProUGUI labelText = CreateText(label + "Label", parent, label, 40f, TextAlignmentOptions.Left, inkColor);
        labelText.rectTransform.anchorMin = new Vector2(0.08f, y);
        labelText.rectTransform.anchorMax = new Vector2(0.68f, y + 0.14f);
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI valueText = CreateText(label + "Value", parent, value.ToString(), 40f, TextAlignmentOptions.Right, inkColor);
        valueText.rectTransform.anchorMin = new Vector2(0.68f, y);
        valueText.rectTransform.anchorMax = new Vector2(0.92f, y + 0.14f);
        valueText.rectTransform.offsetMin = Vector2.zero;
        valueText.rectTransform.offsetMax = Vector2.zero;

        return new ScoreRow
        {
            label = labelText,
            value = valueText
        };
    }

    void ClearRow(ScoreRow row)
    {
        row.label.text = string.Empty;
        row.value.text = string.Empty;
    }

    string GetRankPrefix()
    {
        return string.IsNullOrWhiteSpace(SurgeryResultState.Rank)
            ? "--"
            : SurgeryResultState.Rank.Split(' ')[0];
    }

    string GetRankName()
    {
        if (string.IsNullOrWhiteSpace(SurgeryResultState.Rank))
        {
            return string.Empty;
        }

        int separatorIndex = SurgeryResultState.Rank.IndexOf(" - ", System.StringComparison.Ordinal);
        return separatorIndex >= 0
            ? SurgeryResultState.Rank.Substring(separatorIndex + 3)
            : SurgeryResultState.Rank;
    }

    Color GetRankColor(string rankPrefix)
    {
        switch (rankPrefix)
        {
            case "MS":
            case "S":
                return new Color(0.95f, 0.82f, 0.25f, 1f);
            case "A":
                return new Color(0.75f, 0.98f, 0.56f, 1f);
            case "B":
                return new Color(0.96f, 0.65f, 0.34f, 1f);
            case "C":
                return new Color(0.8f, 0.46f, 0.36f, 1f);
            default:
                return new Color(0.52f, 0.2f, 0.18f, 1f);
        }
    }

    float GetRankPunchScale(string rankPrefix)
    {
        switch (rankPrefix)
        {
            case "MS":
            case "S":
                return 1.45f;
            case "A":
                return 1.3f;
            case "B":
                return 1.18f;
            case "C":
                return 1.1f;
            default:
                return 1.04f;
        }
    }

    bool IsLowRank(string rankPrefix)
    {
        return rankPrefix == "C" || rankPrefix == "D" || rankPrefix == "--";
    }

    string GetSpecialList()
    {
        string list = SurgeryResultState.Breakdown;
        return string.IsNullOrWhiteSpace(list)
            ? "No special notes"
            : list.Replace("Score sources\n", string.Empty).Replace("Cleaned\n", string.Empty);
    }

    void Continue()
    {
        MissionData nextMission = FindNextMission();
        if (nextMission == null)
        {
            GoToEpisodeSelect();
            return;
        }

        MissionFlowState.SetCurrentMission(nextMission);
        SurgeryResultState.Clear();
        SceneManager.LoadScene(nextMission.sceneName);
    }

    void GoToEpisodeSelect()
    {
        SurgeryResultState.Clear();
        SceneManager.LoadScene(missionSelectSceneName);
    }

    void Retry()
    {
        string retryScene = SurgeryResultState.RetrySceneName;
        SurgeryResultState.Clear();
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(retryScene) ? "PatientScene" : retryScene);
    }

    bool HasNextMission()
    {
        return FindNextMission() != null;
    }

    MissionData FindNextMission()
    {
        MissionData currentMission = SurgeryResultState.Mission;
        if (currentMission == null || string.IsNullOrWhiteSpace(currentMission.nextMissionId))
        {
            return null;
        }

        MissionData[] missions = Resources.LoadAll<MissionData>("MissionData");
        for (int i = 0; i < missions.Length; i++)
        {
            MissionData mission = missions[i];
            if (mission != null &&
                string.Equals(mission.missionId, currentMission.nextMissionId, System.StringComparison.Ordinal))
            {
                return mission;
            }
        }

        return null;
    }

    RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    CanvasGroup CreateGroup(string objectName, Transform parent)
    {
        RectTransform rect = CreatePanel(objectName, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return rect.gameObject.AddComponent<CanvasGroup>();
    }

    void SetGroupVisible(CanvasGroup group, bool isVisible)
    {
        group.alpha = isVisible ? 1f : 0f;
        group.interactable = isVisible;
        group.blocksRaycasts = isVisible;
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset readableFont = GetResultFont();
        if (readableFont != null)
        {
            label.font = readableFont;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f);
        label.alignment = alignment;
        label.color = color;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    TMP_FontAsset GetResultFont()
    {
        if (resultFont == null)
        {
            resultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        return resultFont;
    }

    Button CreateButton(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText("Label", rect, labelText, 27f, TextAlignmentOptions.Center, inkColor);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.enableWordWrapping = false;
        return button;
    }

    Sprite CreateResultBackgroundSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                Color color = Color.Lerp(backgroundBottomColor, backgroundTopColor, v);
                float flare = Mathf.Clamp01(1f - Mathf.Abs((u * 1.4f + v * 0.8f) - 0.8f) * 5f);
                color = Color.Lerp(color, new Color(1f, 0.78f, 0.54f, 1f), flare * 0.22f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void EnsureTypewriterAudio(GameObject host)
    {
        if (typewriterAudioSource == null)
        {
            typewriterAudioSource = host.AddComponent<AudioSource>();
            typewriterAudioSource.playOnAwake = false;
            typewriterAudioSource.spatialBlend = 0f;
        }

        if (typewriterClip == null)
        {
            typewriterClip = Resources.Load<AudioClip>("Audio/voi_test");
        }
    }

    void EnsureSceneCamera()
    {
        Camera existingCamera = Camera.main;
        if (existingCamera != null)
        {
            if (existingCamera.GetComponent<AudioListener>() == null &&
                FindAnyObjectByType<AudioListener>() == null)
            {
                existingCamera.gameObject.AddComponent<AudioListener>();
            }

            return;
        }

        GameObject cameraObject = new GameObject("ResultCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundTopColor;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    struct ScoreRow
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    struct ScoreScreenElements
    {
        public ScoreRow stageRow;
        public ScoreRow vitalRow;
        public ScoreRow timeRow;
        public ScoreRow specialRow;
        public TextMeshProUGUI specialList;
    }

    struct FinalScreenElements
    {
        public TextMeshProUGUI rank;
        public TextMeshProUGUI rankName;
        public TextMeshProUGUI operationScore;
    }
}
