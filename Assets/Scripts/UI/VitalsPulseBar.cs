using UnityEngine;
using UnityEngine.UI;

public class VitalsPulseBar : MonoBehaviour
{
    [SerializeField] private Patient patient;
    [SerializeField] private Image pulseBarImage;
    [SerializeField] private RectTransform pulseBarRect;
    [SerializeField] private float healthyBloodLevel = 100f;
    [SerializeField] private float criticalBloodLevel = 20f;
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.85f, 0.3f, 0.9f);
    [SerializeField] private Color criticalColor = new Color(0.9f, 0.15f, 0.15f, 0.95f);
    [SerializeField] private float healthyPulseSpeed = 1.2f;
    [SerializeField] private float criticalPulseSpeed = 4f;
    [SerializeField] private float pulseScaleAmount = 0.08f;

    private Vector3 baseScale = Vector3.one;

    void Start()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        if (pulseBarImage == null)
        {
            pulseBarImage = GetComponent<Image>();
        }

        if (pulseBarRect == null)
        {
            pulseBarRect = transform as RectTransform;
        }

        if (pulseBarRect != null)
        {
            baseScale = pulseBarRect.localScale;
        }
    }

    void Update()
    {
        if (patient == null || pulseBarImage == null || pulseBarRect == null)
        {
            return;
        }

        float blood = Mathf.Clamp(patient.bloodLevel, 0f, healthyBloodLevel);
        float danger = 1f - Mathf.InverseLerp(criticalBloodLevel, healthyBloodLevel, blood);

        pulseBarImage.color = Color.Lerp(healthyColor, criticalColor, danger);

        float pulseSpeed = Mathf.Lerp(healthyPulseSpeed, criticalPulseSpeed, danger);
        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseScaleAmount);

        pulseBarRect.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z);
    }
}
