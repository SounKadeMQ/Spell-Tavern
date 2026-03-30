using TMPro;
using UnityEngine;

public class SurgeryTimer : MonoBehaviour
{
    [SerializeField] private float durationSeconds = 180f;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private SurgeryEndController surgeryEndController;

    private float remainingTime;
    private bool finished;

    void Start()
    {
        remainingTime = Mathf.Max(0f, durationSeconds);
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
}
