using TMPro;
using UnityEngine;

public class SurgeryTimer : MonoBehaviour
{
    [SerializeField] private float durationSeconds = 180f;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private SurgeryEndController surgeryEndController;
    [SerializeField] private Vector2 mobileTimerOffset = new Vector2(-72f, -28f);
    [SerializeField] private Vector2 mobileTimerSize = new Vector2(360f, 96f);

    private float remainingTime;
    private bool finished;

    void Start()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        if (mission != null && mission.patientData != null)
        {
            durationSeconds = mission.patientData.operationTimeLimit;
        }

        remainingTime = Mathf.Max(0f, durationSeconds);
        ApplyMobileTimerPlacement();
        UpdateTimerText();
    }

    void Update()
    {
        if (finished || GameplayPause.IsPaused)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(0f, remainingTime);
        UpdateTimerText();

        if (remainingTime <= 0f)
        {
            finished = true;

            if (surgeryEndController == null)
            {
                surgeryEndController = FindAnyObjectByType<SurgeryEndController>();
            }

            if (surgeryEndController != null)
            {
                surgeryEndController.ShowGameOver();
            }
        }
    }

    void LateUpdate()
    {
        ApplyMobileTimerPlacement();
    }

    void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void ApplyMobileTimerPlacement()
    {
        if (timerText == null || (!Application.isMobilePlatform && Screen.width <= Screen.height))
        {
            return;
        }

        RectTransform timerRect = timerText.rectTransform;
        Canvas canvas = timerText.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect != null && timerRect.parent != canvasRect)
        {
            timerRect.SetParent(canvasRect, false);
        }

        timerRect.anchorMin = new Vector2(1f, 1f);
        timerRect.anchorMax = new Vector2(1f, 1f);
        timerRect.pivot = new Vector2(1f, 1f);
        timerRect.anchoredPosition = mobileTimerOffset;
        timerRect.sizeDelta = mobileTimerSize;
        timerText.alignment = TextAlignmentOptions.TopRight;
        timerText.enableWordWrapping = false;
        timerRect.SetAsLastSibling();
    }
}
