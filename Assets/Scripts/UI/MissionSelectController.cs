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
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI summaryText;
        public GameObject lockedRoot;
        public GameObject completeRoot;
    }

    [SerializeField] private MissionData[] campaignMissions;
    [SerializeField] private MissionButtonBinding[] missionButtons;
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedSummaryText;
    [SerializeField] private Button beginButton;
    [SerializeField] private string titleSceneName = "TitleScene";

    private MissionData selectedMission;

    void Start()
    {
        if (campaignMissions == null || campaignMissions.Length == 0)
        {
            campaignMissions = Resources.LoadAll<MissionData>("MissionData");
            System.Array.Sort(campaignMissions, (a, b) => string.Compare(a.missionId, b.missionId, System.StringComparison.Ordinal));
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
    }

    public void BeginSelectedMission()
    {
        if (selectedMission == null)
        {
            return;
        }

        MissionFlowState.SetCurrentMission(selectedMission);
        SceneManager.LoadScene(selectedMission.sceneName);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
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

            if (binding.summaryText != null)
            {
                binding.summaryText.text = mission != null ? mission.missionSummary : string.Empty;
            }

            if (binding.lockedRoot != null)
            {
                binding.lockedRoot.SetActive(!unlocked);
            }

            if (binding.completeRoot != null)
            {
                binding.completeRoot.SetActive(completed);
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
        background.color = new Color(0.06f, 0.07f, 0.09f, 1f);

        RectTransform root = canvas.GetComponent<RectTransform>();
        RectTransform header = CreatePanel("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -90f), new Vector2(0f, 140f));
        Image headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.11f, 0.08f, 0.07f, 0.95f);

        TextMeshProUGUI title = CreateText("Title", header, "SPELL TAVERN", 44, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 0f);
        title.rectTransform.anchorMax = new Vector2(0.55f, 1f);
        title.rectTransform.offsetMin = new Vector2(42f, 10f);
        title.rectTransform.offsetMax = new Vector2(-10f, -10f);

        TextMeshProUGUI subtitle = CreateText("Subtitle", header, "Chapter 1 patient board", 22, TextAlignmentOptions.Right);
        subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitle.rectTransform.offsetMin = new Vector2(10f, 10f);
        subtitle.rectTransform.offsetMax = new Vector2(-42f, -10f);

        RectTransform listRoot = CreatePanel("MissionList", root, new Vector2(0f, 0f), new Vector2(0.58f, 1f), new Vector2(34f, 30f), new Vector2(-18f, -170f));
        RectTransform detailRoot = CreatePanel("MissionDetail", root, new Vector2(0.58f, 0f), new Vector2(1f, 1f), new Vector2(18f, 30f), new Vector2(-34f, -170f));
        Image detailImage = detailRoot.gameObject.AddComponent<Image>();
        detailImage.color = new Color(0.13f, 0.14f, 0.15f, 0.9f);

        selectedTitleText = CreateText("SelectedTitle", detailRoot, string.Empty, 28, TextAlignmentOptions.TopLeft);
        selectedTitleText.rectTransform.anchorMin = new Vector2(0f, 0.7f);
        selectedTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        selectedTitleText.rectTransform.offsetMin = new Vector2(24f, 0f);
        selectedTitleText.rectTransform.offsetMax = new Vector2(-24f, -24f);

        selectedSummaryText = CreateText("SelectedSummary", detailRoot, string.Empty, 20, TextAlignmentOptions.TopLeft);
        selectedSummaryText.rectTransform.anchorMin = new Vector2(0f, 0.2f);
        selectedSummaryText.rectTransform.anchorMax = new Vector2(1f, 0.72f);
        selectedSummaryText.rectTransform.offsetMin = new Vector2(24f, 0f);
        selectedSummaryText.rectTransform.offsetMax = new Vector2(-24f, 0f);

        beginButton = CreateButton("BeginButton", detailRoot, "Begin", new Vector2(0.08f, 0.06f), new Vector2(0.52f, 0.16f));
        beginButton.onClick.AddListener(BeginSelectedMission);

        Button backButton = CreateButton("BackButton", detailRoot, "Back", new Vector2(0.58f, 0.06f), new Vector2(0.92f, 0.16f));
        backButton.onClick.AddListener(BackToTitle);

        missionButtons = new MissionButtonBinding[campaignMissions.Length];
        for (int i = 0; i < campaignMissions.Length; i++)
        {
            RectTransform slot = CreatePanel("MissionSlot_" + i, listRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f - (i * 118f)), new Vector2(0f, -28f - (i * 118f)));
            Image slotImage = slot.gameObject.AddComponent<Image>();
            slotImage.color = new Color(0.15f, 0.12f, 0.1f, 0.92f);

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slotImage;

            TextMeshProUGUI missionTitle = CreateText("MissionTitle", slot, string.Empty, 22, TextAlignmentOptions.TopLeft);
            missionTitle.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            missionTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            missionTitle.rectTransform.offsetMin = new Vector2(18f, 0f);
            missionTitle.rectTransform.offsetMax = new Vector2(-18f, -8f);

            TextMeshProUGUI missionSummary = CreateText("MissionSummary", slot, string.Empty, 15, TextAlignmentOptions.TopLeft);
            missionSummary.rectTransform.anchorMin = new Vector2(0f, 0f);
            missionSummary.rectTransform.anchorMax = new Vector2(1f, 0.52f);
            missionSummary.rectTransform.offsetMin = new Vector2(18f, 8f);
            missionSummary.rectTransform.offsetMax = new Vector2(-18f, 0f);

            GameObject lockedRoot = CreateBadge("Locked", slot, "LOCKED", new Color(0.28f, 0.08f, 0.08f, 0.95f));
            GameObject completeRoot = CreateBadge("Complete", slot, "CLEAR", new Color(0.08f, 0.24f, 0.16f, 0.95f));

            missionButtons[i] = new MissionButtonBinding
            {
                mission = campaignMissions[i],
                button = button,
                titleText = missionTitle,
                summaryText = missionSummary,
                lockedRoot = lockedRoot,
                completeRoot = completeRoot
            };
        }
    }

    Canvas CreateCanvas(string objectName)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
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

        TextMeshProUGUI label = CreateText("Label", buttonRect, labelText, 22, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    GameObject CreateBadge(string objectName, Transform parent, string labelText, Color color)
    {
        RectTransform badge = CreatePanel(objectName, parent, new Vector2(0.78f, 0.62f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero);
        Image image = badge.gameObject.AddComponent<Image>();
        image.color = color;

        TextMeshProUGUI label = CreateText("Label", badge, labelText, 14, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return badge.gameObject;
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
