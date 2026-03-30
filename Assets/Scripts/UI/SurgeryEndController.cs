using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private bool autoCompleteWhenAllWoundsClosed = true;

    private bool hasEnded;

    void Start()
    {
        ResolveSceneReferences();
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

        if (!autoCompleteWhenAllWoundsClosed || patientWounds == null)
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

        hasEnded = true;
        GameplayPause.SetPaused(true);
        RefreshResultTexts(completeScoreText, completeRankText, completeMissText);
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

        hasEnded = true;
        GameplayPause.SetPaused(true);
        RefreshResultTexts(gameOverScoreText, gameOverRankText, gameOverMissText);
        SetPanelVisible(missionCompleteRoot, false);
        SetPanelVisible(gameOverRoot, true);
        BringPanelToFront(gameOverRoot);
    }

    public void RetrySurgery()
    {
        GameplayPause.SetPaused(false);
        SceneManager.LoadScene(retrySceneName);
    }

    public void BackToTitle()
    {
        GameplayPause.SetPaused(false);
        SceneManager.LoadScene(titleSceneName);
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
}
