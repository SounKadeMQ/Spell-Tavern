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

    void Awake()
    {
        healthComponent = GetComponent<Health>();

        if(healthComponent == null)
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

        slider.maxValue = healthComponent.GetMaxHealth();
        slider.value = healthComponent.GetCurrentHealth();
        
    }

    void Start()
    {
        
    }

    void Update()
    {
        slider.value = healthComponent.GetCurrentHealth();
        if (!useExistingHealthBar && healthBarInstance != null)
        {
            healthBarInstance.transform.position = healthComponent.transform.position + offset;
        }  
    }
}
