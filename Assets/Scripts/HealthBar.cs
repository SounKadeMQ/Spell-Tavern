using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private bool useExistingHealthBar = false;
    [SerializeField] private Slider existingSlider;

    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    
    private Slider slider;
    private GameObject healthBarInstance;
    private Health healthComponent;
    private Patient patient;

    void Awake()
    {
        healthComponent = GetComponent<Health>();
        patient = GetComponent<Patient>();

        if(patient == null)
        {
            enabled = false;
            return;
        }

        if(useExistingHealthBar == false)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + offset, Quaternion.identity, transform);
            slider = healthBarInstance.GetComponentInChildren<Slider>();
        }
        else if(useExistingHealthBar == true)
        {
            slider = existingSlider;
        }

        slider.maxValue = patient.bloodLevel;
        slider.value = patient.bloodLevel;
        
    }

    void Start()
    {
        
    }

    void Update()
    {
        slider.value = patient.bloodLevel;
        if (!useExistingHealthBar && healthBarInstance != null)
        {
            healthBarInstance.transform.position = healthComponent.transform.position + offset;
        }  
    }
}
