using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionSelectController : MonoBehaviour
{
    [System.Serializable]
    public class MissionButtonBinding
    {
        public MissionData mission;
        public Button button;
        public TextMeshProUGUI episodeCodeText;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI summaryText;
        public Image backgroundImage;
        public GameObject selectedRoot;
        public GameObject lockedRoot;
        public GameObject completeRoot;
    }

    [SerializeField] private MissionData[] campaignMissions;
    [SerializeField] private MissionButtonBinding[] missionButtons;
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedSummaryText;
    [SerializeField] private Button beginButton;
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private Color normalSlotColor = new Color(0.76f, 0.76f, 0.72f, 0.94f);
    [SerializeField] private Color selectedSlotColor = new Color(0.9f, 0.98f, 0.88f, 1f);
    [SerializeField] private Color lockedSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.72f);
    [SerializeField] private Color completeSlotColor = new Color(0.82f, 0.84f, 0.8f, 0.94f);
    [SerializeField] private Color preOpBackgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);
    [SerializeField] private float sceneFadeDuration = 0.45f;

    private MissionData selectedMission;
    private CanvasGroup sceneFadeGroup;
    private bool isLoadingScene;

    void Update()
    {
        if (isLoadingScene)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            BeginSelectedMission();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToTitle();
        }
    }

    void Start()
    {
        if (campaignMissions == null || campaignMissions.Length == 0)
        {
            campaignMissions = Resources.LoadAll<MissionData>("MissionData");
            System.Array.Sort(campaignMissions, CompareMissions);
        }

        if (missionButtons == null || missionButtons.Length == 0)
        {
            BuildDefaultLayout();
        }

        RefreshMissionButtons();
        SelectFirstUnlockedMission();
    }

    public void SelectMission(MissionData mission)
    {
        if (!MissionFlowState.IsUnlocked(mission, campaignMissions))
        {
            return;
        }

        selectedMission = mission;

        if (selectedTitleText != null)
        {
            selectedTitleText.text = mission != null ? mission.displayName : string.Empty;
        }

        if (selectedSummaryText != null)
        {
            selectedSummaryText.text = mission != null ? mission.missionSummary : string.Empty;
        }

        if (beginButton != null)
        {
            beginButton.interactable = selectedMission != null;
        }

        UpdateSelectedVisuals();
    }

    public void BeginSelectedMission()
    {
        if (selectedMission == null)
        {
            return;
        }

        MissionFlowState.SetCurrentMission(selectedMission);
        StartCoroutine(LoadSceneWithFade(selectedMission.sceneName));
    }

    public void BackToTitle()
    {
        StartCoroutine(LoadSceneWithFade(titleSceneName));
    }

    void RefreshMissionButtons()
    {
        if (missionButtons == null)
        {
            return;
        }

        for (int i = 0; i < missionButtons.Length; i++)
        {
            MissionButtonBinding binding = missionButtons[i];
            if (binding == null)
            {
                continue;
            }

            MissionData mission = binding.mission;
            bool unlocked = MissionFlowState.IsUnlocked(mission, campaignMissions);
            bool completed = MissionFlowState.IsCompleted(mission);

            if (binding.titleText != null)
            {
                binding.titleText.text = mission != null ? mission.displayName : "Empty Slot";
            }

            if (binding.episodeCodeText != null)
            {
                binding.episodeCodeText.text = mission != null ? mission.episodeCode : string.Empty;
            }

            if (binding.summaryText != null)
            {
                binding.summaryText.text = mission != null ? mission.missionSummary : string.Empty;
            }

            EnsureRankText(binding);
            if (binding.rankText != null)
            {
                binding.rankText.text = GetMissionRankLabel(mission, completed);
                binding.rankText.gameObject.SetActive(completed);
            }

            if (binding.lockedRoot != null)
            {
                binding.lockedRoot.SetActive(!unlocked);
            }

            if (binding.completeRoot != null)
            {
                binding.completeRoot.SetActive(completed);
            }

            if (binding.backgroundImage != null)
            {
                binding.backgroundImage.color = GetSlotColor(mission, unlocked, completed);
            }

            if (binding.selectedRoot != null)
            {
                binding.selectedRoot.SetActive(mission != null && mission == selectedMission);
            }

            if (binding.button != null)
            {
                binding.button.interactable = unlocked;
                MissionData capturedMission = mission;
                binding.button.onClick.RemoveListener(() => SelectMission(capturedMission));
                binding.button.onClick.AddListener(() => SelectMission(capturedMission));
            }
        }
    }

    void SelectFirstUnlockedMission()
    {
        if (campaignMissions == null)
        {
            SelectMission(null);
            return;
        }

        for (int i = 0; i < campaignMissions.Length; i++)
        {
            if (MissionFlowState.IsUnlocked(campaignMissions[i], campaignMissions))
            {
                SelectMission(campaignMissions[i]);
                return;
            }
        }

        SelectMission(null);
    }

    void BuildDefaultLayout()
    {
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("MissionSelectCanvas");
        Image background = canvas.gameObject.AddComponent<Image>();
        background.color = preOpBackgroundColor;

        RectTransform root = canvas.GetComponent<RectTransform>();
        RectTransform header = CreatePanel("Header", root, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        Image headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.14f, 0.08f, 0.04f, 0.62f);

        TextMeshProUGUI title = CreateText("Title", header, "EPISODE SELECT", 80, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 0f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(56f, 8f);
        title.rectTransform.offsetMax = new Vector2(-56f, -8f);
        title.color = new Color(0.86f, 0.77f, 0.62f, 1f);

        RectTransform listRoot = CreatePanel("EpisodeList", root, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.83f), Vector2.zero, Vector2.zero);
        RectTransform detailRoot = CreatePanel("EpisodeDetail", root, new Vector2(0.6f, 0.12f), new Vector2(0.96f, 0.83f), Vector2.zero, Vector2.zero);
        Image detailImage = detailRoot.gameObject.AddComponent<Image>();
        detailImage.color = new Color(0.12f, 0.135f, 0.135f, 0.96f);
        detailRoot.gameObject.SetActive(false);

        selectedTitleText = CreateText("SelectedTitle", detailRoot, string.Empty, 30, TextAlignmentOptions.TopLeft);
        selectedTitleText.rectTransform.anchorMin = new Vector2(0f, 0.74f);
        selectedTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        selectedTitleText.rectTransform.offsetMin = new Vector2(26f, 0f);
        selectedTitleText.rectTransform.offsetMax = new Vector2(-26f, -26f);

        selectedSummaryText = CreateText("SelectedSummary", detailRoot, string.Empty, 22, TextAlignmentOptions.TopLeft);
        selectedSummaryText.rectTransform.anchorMin = new Vector2(0f, 0.22f);
        selectedSummaryText.rectTransform.anchorMax = new Vector2(1f, 0.72f);
        selectedSummaryText.rectTransform.offsetMin = new Vector2(26f, 0f);
        selectedSummaryText.rectTransform.offsetMax = new Vector2(-26f, 0f);

        beginButton = CreateButton("BeginButton", root, "OK", new Vector2(0.68f, 0.025f), new Vector2(0.81f, 0.075f));
        beginButton.onClick.AddListener(BeginSelectedMission);

        Button backButton = CreateButton("BackButton", root, "Back", new Vector2(0.83f, 0.025f), new Vector2(0.96f, 0.075f));
        backButton.onClick.AddListener(BackToTitle);

        missionButtons = new MissionButtonBinding[campaignMissions.Length];
        for (int i = 0; i < campaignMissions.Length; i++)
        {
            RectTransform slot = CreatePanel("EpisodeSlot_" + i, listRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -116f - (i * 140f)), new Vector2(0f, -8f - (i * 140f)));
            Image slotImage = slot.gameObject.AddComponent<Image>();
            slotImage.color = normalSlotColor;

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slotImage;

            GameObject selectedRoot = CreateSelectedFrame(slot);

            TextMeshProUGUI episodeCode = CreateText("EpisodeCode", slot, string.Empty, 36, TextAlignmentOptions.Center);
            episodeCode.rectTransform.anchorMin = new Vector2(0f, 0f);
            episodeCode.rectTransform.anchorMax = new Vector2(0.14f, 1f);
            episodeCode.rectTransform.offsetMin = new Vector2(4f, 0f);
            episodeCode.rectTransform.offsetMax = new Vector2(-4f, 0f);
            episodeCode.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            TextMeshProUGUI missionTitle = CreateText("MissionTitle", slot, string.Empty, 40, TextAlignmentOptions.Left);
            missionTitle.rectTransform.anchorMin = new Vector2(0.14f, 0f);
            missionTitle.rectTransform.anchorMax = new Vector2(0.7f, 1f);
            missionTitle.rectTransform.offsetMin = new Vector2(12f, 0f);
            missionTitle.rectTransform.offsetMax = new Vector2(-10f, 0f);
            missionTitle.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            missionTitle.enableAutoSizing = true;
            missionTitle.fontSizeMin = 30f;
            missionTitle.fontSizeMax = 40f;
            missionTitle.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI rankText = CreateText("MissionRank", slot, string.Empty, 30, TextAlignmentOptions.Center);
            rankText.rectTransform.anchorMin = new Vector2(0.7f, 0f);
            rankText.rectTransform.anchorMax = new Vector2(0.79f, 1f);
            rankText.rectTransform.offsetMin = Vector2.zero;
            rankText.rectTransform.offsetMax = Vector2.zero;
            rankText.color = new Color(0.08f, 0.12f, 0.11f, 1f);
            rankText.enableWordWrapping = false;
            rankText.fontStyle = FontStyles.Bold;

            GameObject lockedRoot = CreateBadge("Locked", slot, "LOCKED", new Color(0.28f, 0.08f, 0.08f, 0.95f));
            GameObject completeRoot = CreateBadge("Complete", slot, "CLEAR", new Color(0.08f, 0.24f, 0.16f, 0.95f));

            missionButtons[i] = new MissionButtonBinding
            {
                mission = campaignMissions[i],
                button = button,
                episodeCodeText = episodeCode,
                titleText = missionTitle,
                rankText = rankText,
                backgroundImage = slotImage,
                selectedRoot = selectedRoot,
                lockedRoot = lockedRoot,
                completeRoot = completeRoot
            };
        }

        CreateSceneFade(root);
        StartCoroutine(FadeSceneOverlay(1f, 0f));
    }

    Canvas CreateCanvas(string objectName)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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

        TextMeshProUGUI label = CreateText("Label", buttonRect, labelText, 44, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    GameObject CreateBadge(string objectName, Transform parent, string labelText, Color color)
    {
        RectTransform badge = CreatePanel(objectName, parent, new Vector2(0.81f, 0.16f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
        Image image = badge.gameObject.AddComponent<Image>();
        image.color = color;

        TextMeshProUGUI label = CreateText("Label", badge, labelText, 28, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return badge.gameObject;
    }

    GameObject CreateSelectedFrame(Transform parent)
    {
        RectTransform frame = CreatePanel("SelectedFrame", parent, Vector2.zero, Vector2.one, new Vector2(-4f, -4f), new Vector2(4f, 4f));
        Image image = frame.gameObject.AddComponent<Image>();
        image.color = new Color(0.32f, 0.95f, 0.38f, 0.72f);
        image.raycastTarget = false;
        frame.SetAsFirstSibling();
        frame.gameObject.SetActive(false);
        return frame.gameObject;
    }

    void UpdateSelectedVisuals()
    {
        if (missionButtons == null)
        {
            return;
        }

        for (int i = 0; i < missionButtons.Length; i++)
        {
            MissionButtonBinding binding = missionButtons[i];
            if (binding == null)
            {
                continue;
            }

            bool unlocked = MissionFlowState.IsUnlocked(binding.mission, campaignMissions);
            bool completed = MissionFlowState.IsCompleted(binding.mission);

            if (binding.backgroundImage != null)
            {
                binding.backgroundImage.color = GetSlotColor(binding.mission, unlocked, completed);
            }

            if (binding.selectedRoot != null)
            {
                binding.selectedRoot.SetActive(binding.mission != null && binding.mission == selectedMission);
            }
        }
    }

    Color GetSlotColor(MissionData mission, bool unlocked, bool completed)
    {
        if (!unlocked)
        {
            return lockedSlotColor;
        }

        if (mission != null && mission == selectedMission)
        {
            return selectedSlotColor;
        }

        return completed ? completeSlotColor : normalSlotColor;
    }

    void EnsureRankText(MissionButtonBinding binding)
    {
        if (binding == null || binding.rankText != null)
        {
            return;
        }

        Transform parent = binding.backgroundImage != null
            ? binding.backgroundImage.transform
            : (binding.button != null ? binding.button.transform : null);

        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find("MissionRank");
        if (existing != null)
        {
            binding.rankText = existing.GetComponent<TextMeshProUGUI>();
            if (binding.rankText != null)
            {
                return;
            }
        }

        binding.rankText = CreateText("MissionRank", parent, string.Empty, 30, TextAlignmentOptions.Center);
        binding.rankText.rectTransform.anchorMin = new Vector2(0.7f, 0f);
        binding.rankText.rectTransform.anchorMax = new Vector2(0.79f, 1f);
        binding.rankText.rectTransform.offsetMin = Vector2.zero;
        binding.rankText.rectTransform.offsetMax = Vector2.zero;
        binding.rankText.color = new Color(0.08f, 0.12f, 0.11f, 1f);
        binding.rankText.enableWordWrapping = false;
        binding.rankText.fontStyle = FontStyles.Bold;
    }

    string GetMissionRankLabel(MissionData mission, bool completed)
    {
        if (!completed)
        {
            return string.Empty;
        }

        string rank = MissionFlowState.GetRank(mission);
        if (string.IsNullOrWhiteSpace(rank))
        {
            return mission != null && mission.kind == MissionData.MissionKind.Intermission ? "CLR" : "--";
        }

        int separatorIndex = rank.IndexOf(" - ", System.StringComparison.Ordinal);
        return separatorIndex > 0 ? rank.Substring(0, separatorIndex) : rank;
    }

    int CompareMissions(MissionData left, MissionData right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int orderComparison = left.episodeOrder.CompareTo(right.episodeOrder);
        if (orderComparison != 0)
        {
            return orderComparison;
        }

        return string.Compare(left.missionId, right.missionId, System.StringComparison.Ordinal);
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

    IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (isLoadingScene || string.IsNullOrEmpty(sceneName))
        {
            yield break;
        }

        isLoadingScene = true;
        yield return FadeSceneOverlay(sceneFadeGroup != null ? sceneFadeGroup.alpha : 0f, 1f);
        SceneManager.LoadScene(sceneName);
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
