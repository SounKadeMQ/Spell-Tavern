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
        healthBar.value = patient.health;
        bloodBar.value = patient.bloodLevel;

        if (vitalsText != null)
        {
            vitalsText.text =
                $"HP: {patient.health:F0}\n" +
                $"Blood: {patient.bloodLevel:F0}\n" +
                $"Bleeding: {patient.bleed}";
        }
    }
}


