using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SurgeryEndController : MonoBehaviour
{
    [SerializeField] private Patient patient;
    [SerializeField] private PatientWounds patientWounds;
    [SerializeField] private SpellController spellController;

    [Header("Panels")]
    [SerializeField] private GameObject missionCompleteRoot;
    [SerializeField] private GameObject gameOverRoot;

    [Header("Mission Complete UI")]
    [SerializeField] private TextMeshProUGUI completeScoreText;
    [SerializeField] private TextMeshProUGUI completeRankText;
    [SerializeField] private TextMeshProUGUI completeMissText;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverRankText;
    [SerializeField] private TextMeshProUGUI gameOverMissText;

    [Header("Scene Flow")]
    [SerializeField] private string retrySceneName = "PatientScene";
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string missionSelectSceneName = "ChapterSelect";
    [SerializeField] private string resultSceneName = "SurgeryResultScene";
    [SerializeField] private bool autoCompleteWhenAllWoundsClosed = true;

    private bool hasEnded;
    private bool usesScriptedCompletion;
    private bool missionCompleteNavigationBuilt;

    void Start()
    {
        ResolveSceneReferences();
        usesScriptedCompletion = CurrentMissionUsesScriptedCompletion();
        SetPanelVisible(missionCompleteRoot, false);
        SetPanelVisible(gameOverRoot, false);
    }

    void OnEnable()
    {
        Patient.PatientDied += HandlePatientDied;
    }

    void OnDisable()
    {
        Patient.PatientDied -= HandlePatientDied;
    }

    void Update()
    {
        if (hasEnded)
        {
            return;
        }

        ResolveSceneReferences();

        if (!autoCompleteWhenAllWoundsClosed || usesScriptedCompletion || patientWounds == null)
        {
            return;
        }

        if (patient != null && patient.IsDead)
        {
            return;
        }

        if (patientWounds.GetOpenWoundCount() == 0)
        {
            ShowMissionComplete();
        }
    }

    public void ShowMissionComplete()
    {
        if (hasEnded)
        {
            return;
        }

        EmergencyTimeDilationEffect.Deactivate();
        hasEnded = true;
        string rank = spellController != null ? spellController.GetCurrentScoreRank() : string.Empty;
        MissionFlowState.MarkCompleted(MissionFlowState.CurrentMission, rank);
        SurgeryTimer timer = FindAnyObjectByType<SurgeryTimer>();
        SurgeryResultState.Capture(MissionFlowState.CurrentMission, spellController, patient, timer, retrySceneName);
        GameplayPause.SetPaused(true);
        SceneManager.LoadScene(resultSceneName);
    }

    public void ShowGameOver()
    {
        if (hasEnded)
        {
            return;
        }

        EmergencyTimeDilationEffect.Deactivate();
        hasEnded = true;
        GameplayPause.SetPaused(true);
        RefreshResultTexts(gameOverScoreText, gameOverRankText, ref gameOverMissText, false);
        SetPanelVisible(missionCompleteRoot, false);
        SetPanelVisible(gameOverRoot, true);
        BringPanelToFront(gameOverRoot);
    }

    public void RetrySurgery()
    {
        EmergencyTimeDilationEffect.Deactivate();
        GameplayPause.SetPaused(false);
        SceneManager.LoadScene(retrySceneName);
    }

    public void BackToTitle()
    {
        EmergencyTimeDilationEffect.Deactivate();
        GameplayPause.SetPaused(false);
        SceneManager.LoadScene(titleSceneName);
    }

    public void ContinueToMissionSelect()
    {
        EmergencyTimeDilationEffect.Deactivate();
        GameplayPause.SetPaused(false);
        SceneManager.LoadScene(missionSelectSceneName);
    }

    public void ContinueToNextMission()
    {
        MissionData nextMission = FindNextMission();
        if (nextMission == null)
        {
            ContinueToMissionSelect();
            return;
        }

        EmergencyTimeDilationEffect.Deactivate();
        GameplayPause.SetPaused(false);
        MissionFlowState.SetCurrentMission(nextMission);
        SceneManager.LoadScene(nextMission.sceneName);
    }

    void HandlePatientDied(Patient deadPatient)
    {
        if (patient != null && deadPatient != patient)
        {
            return;
        }

        ShowGameOver();
    }

    void RefreshResultTexts(TextMeshProUGUI scoreText, TextMeshProUGUI rankText, ref TextMeshProUGUI detailText, bool missionComplete)
    {
        if (spellController == null)
        {
            return;
        }

        detailText = EnsureResultDetailText(detailText, rankText, missionComplete ? "CompleteBreakdownText" : "GameOverBreakdownText");

        PrepareResultText(scoreText, 26f, 40f, TextAlignmentOptions.Center);
        PrepareResultText(rankText, 22f, 34f, TextAlignmentOptions.Center);
        PrepareResultText(detailText, 15f, 22f, TextAlignmentOptions.TopLeft);

        if (scoreText != null)
        {
            scoreText.text = (missionComplete ? "Mission Complete" : "Game Over") +
                             "\nScore " + spellController.GetScore();
        }

        if (rankText != null)
        {
            rankText.text = "Rank " + spellController.GetCurrentScoreRank() +
                            "   Misses " + spellController.GetMissCount();
        }

        if (detailText != null)
        {
            detailText.text = missionComplete
                ? spellController.GetMissionCompleteBreakdown()
                : spellController.GetGameOverBreakdown();
        }
    }

    void ResolveSceneReferences()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        if (patientWounds == null)
        {
            if (patient != null)
            {
                patientWounds = patient.GetComponent<PatientWounds>();
            }

            if (patientWounds == null)
            {
                patientWounds = FindAnyObjectByType<PatientWounds>();
            }
        }

        if (spellController == null)
        {
            spellController = FindAnyObjectByType<SpellController>();
        }
    }

    bool CurrentMissionUsesScriptedCompletion()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        if (mission == null || mission.surgeryLines == null)
        {
            return false;
        }

        for (int i = 0; i < mission.surgeryLines.Length; i++)
        {
            PatientDialogueLine line = mission.surgeryLines[i];
            if (line != null && line.missionCompleteFlag)
            {
                return true;
            }
        }

        return false;
    }

    void SetPanelVisible(GameObject root, bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
    }

    void BringPanelToFront(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        RectTransform rectTransform = root.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.SetAsLastSibling();
        }
    }

    void EnsureMissionCompleteNavigation()
    {
        if (missionCompleteNavigationBuilt || missionCompleteRoot == null)
        {
            return;
        }

        RectTransform rootRect = missionCompleteRoot.transform as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        RectTransform mainMenuButton = FindChildRect(rootRect, "MainMenuButton");
        if (mainMenuButton != null)
        {
            mainMenuButton.anchorMin = new Vector2(0.5f, 0.5f);
            mainMenuButton.anchorMax = new Vector2(0.5f, 0.5f);
            mainMenuButton.pivot = new Vector2(0.5f, 0.5f);
            mainMenuButton.anchoredPosition = new Vector2(-140f, -150f);
            mainMenuButton.sizeDelta = new Vector2(220f, 64f);
            MissionData nextMission = FindNextMission();
            SetButtonLabel(mainMenuButton, nextMission != null ? "Next" : "Chapter Select");
            Button nextButton = mainMenuButton.GetComponent<Button>();
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(ContinueToNextMission);
            }
        }

        if (FindChildRect(rootRect, "RetryButton") == null)
        {
            Button retryButton = CreateNavigationButton(rootRect, "RetryButton", "Retry", new Vector2(140f, -150f));
            retryButton.onClick.AddListener(RetrySurgery);
        }

        missionCompleteNavigationBuilt = true;
    }

    MissionData FindNextMission()
    {
        MissionData currentMission = MissionFlowState.CurrentMission;
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

    RectTransform FindChildRect(Transform root, string childName)
    {
        RectTransform[] rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTransform = rectTransforms[i];
            if (rectTransform != null && rectTransform.name == childName)
            {
                return rectTransform;
            }
        }

        return null;
    }

    Button CreateNavigationButton(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(220f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.48f, 0.08f, 0.045f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonRect, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.92f, 0.86f, 0.76f, 1f);
        label.enableWordWrapping = false;

        return button;
    }

    void SetButtonLabel(RectTransform buttonRect, string labelText)
    {
        if (buttonRect == null)
        {
            return;
        }

        TextMeshProUGUI label = buttonRect.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = labelText;
        }
    }

    TextMeshProUGUI EnsureResultDetailText(TextMeshProUGUI detailText, TextMeshProUGUI anchorText, string objectName)
    {
        if (detailText != null)
        {
            return detailText;
        }

        Transform parent = anchorText != null ? anchorText.transform.parent : null;
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
        {
            return existingText;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -42f);
        rect.sizeDelta = new Vector2(760f, 170f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.color = new Color(0.92f, 0.86f, 0.76f, 1f);
        return text;
    }

    void PrepareResultText(TextMeshProUGUI text, float minSize, float maxSize, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.rectTransform.localScale = Vector3.one;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = alignment;
        text.color = new Color(0.92f, 0.86f, 0.76f, 1f);
    }
}
