using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VitalsUI : MonoBehaviour
{
    public int test = 5;
    public Patient patient;

    [Header("UI")] 
    public Slider healthBar;
    public Slider bloodBar;
    public TextMeshProUGUI vitalsText;

    void Update()
    {
        if (patient == null) return;
        bloodBar.value = patient.bloodLevel;

        float bleed = patient.getBleedRate();

        if (vitalsText != null)
        {
            vitalsText.text =
                $"Blood: {patient.bloodLevel:F0}\n" +
                $"Bleeding: " + (patient.bleed ? "YES":"NO") + "\n" +
                $"Bleed/sec: " + bleed.ToString("F2");
        }
    }
}


