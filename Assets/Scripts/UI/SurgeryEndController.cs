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
        GameplayPause.SetPaused(true);
        RefreshResultTexts(completeScoreText, completeRankText, completeMissText);
        EnsureMissionCompleteNavigation();
        SetPanelVisible(gameOverRoot, false);
        SetPanelVisible(missionCompleteRoot, true);
        BringPanelToFront(missionCompleteRoot);
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
        RefreshResultTexts(gameOverScoreText, gameOverRankText, gameOverMissText);
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

    void HandlePatientDied(Patient deadPatient)
    {
        if (patient != null && deadPatient != patient)
        {
            return;
        }

        ShowGameOver();
    }

    void RefreshResultTexts(TextMeshProUGUI scoreText, TextMeshProUGUI rankText, TextMeshProUGUI missText)
    {
        if (spellController == null)
        {
            return;
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + spellController.GetScore();
        }

        if (rankText != null)
        {
            rankText.text = spellController.GetCurrentScoreRank();
        }

        if (missText != null)
        {
            missText.text = "Misses: " + spellController.GetMissCount();
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
        }

        if (FindChildRect(rootRect, "ChapterSelectButton") == null)
        {
            Button chapterSelectButton = CreateNavigationButton(rootRect, "ChapterSelectButton", "Chapter Select", new Vector2(140f, -150f));
            chapterSelectButton.onClick.AddListener(ContinueToMissionSelect);
        }

        missionCompleteNavigationBuilt = true;
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
}
